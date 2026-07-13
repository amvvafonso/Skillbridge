using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;
using Skillbridge.Data;
using Skillbridge.Hubs;
using Skillbridge.Models;
using Skillbridge.Models.Client;
using Skillbridge.Models.Project;
using Skillbridge.Utilities;

namespace Skillbridge.Services;

public interface IOrganizationService
{
    Task<Result> CreateOrganizationAsync(string userId, string organizationName, string organizationAddress, string organizationDescription, IFormFile? logo);
    Task<Result> EditOrganizationAsync(string organizationId, string userId, string? name, string? address,  string? description, IFormFile? logo, IFormFile? banner);
    Task<Result> DeleteOrganizationAsync(string orgId, string userId);
    Task<Result> DeleteProjectAsync(string projectId, string userId);
    Task<Result> AddMemberAsync(string organizationId, string memberEmail);
    Task<Result> DeleteMemberAsync(string memberId, string organizationId,  string userId);
    Task<Result> PromoteMemberAsync(string memberId, string organizationId, string userId);
    Task<Result> CreatePostAsync(string organizationId, string newPostTitle, string newPostContent, string userId);
    Task<Result> DeletePostAsync(string postId, string userId);
    // Utils
    Task<OrganizationMember?> MemberBelongsToOrganization(string orgId, string userId);
}


public class OrganizationService(ApplicationDbContext context, IS3Api iS3Api, INotificationService notificationService) : IOrganizationService
{
    public async Task<Result> CreateOrganizationAsync(string userId, string organizationName, string organizationAddress, string organizationDescription, IFormFile? logo) {
        if (string.IsNullOrEmpty(organizationName) || string.IsNullOrEmpty(organizationAddress) ||
            string.IsNullOrEmpty(organizationDescription))
            return Result.Fail("É obrigatório preencher todos os campos", ErrorType.MissingComponent);

        var newGuid =  Guid.NewGuid().ToString();
        var logoPath = "default_logo.png";

        if (logo != null && logo.Length > 0)
        {
            using var ms = new MemoryStream();
            await logo.CopyToAsync(ms);
            var bytes = ms.ToArray();
            
            var ext =  Path.GetExtension(logo.FileName);
            var uploaded = await iS3Api.UploadBinaryAsync("logos",  $"{newGuid}{ext}" , bytes,  logo.ContentType );
            if (uploaded) logoPath = $"{newGuid}{ext}";
        }

        var organization = new Organization
        {
            OrganizationId = newGuid,
            OrganizationName = organizationName,
            OrganizationAddress = organizationAddress,
            Owner = userId,
            LogoPath = logoPath,
            BannerPath = "default_banner.png",
        };
        context.Organizations.Add(organization);
        context.OrganizationMembers.Add(new OrganizationMember(Guid.NewGuid().ToString(), newGuid, userId, Role.Owner));
        await context.SaveChangesAsync();

        return Result.Ok(message: "Organização criada com sucesso!");
    }
    
    private static readonly string[] AllowedImageExtensions = [".png", ".jpg", ".jpeg", ".webp"];
    public async Task<Result> EditOrganizationAsync(string organizationId, string userId, string? name, string? address, string? description, IFormFile? logo, IFormFile? banner)
    {
        var permission = await MemberBelongsToOrganization(organizationId, userId);
        if (permission == null || permission.Role != Role.Owner) return Result.Fail("Apenas o dono pode editar a organização.", ErrorType.Denied);

        var organization = await GetOrganization(organizationId);
        if (organization == null) return Result.Fail("Organização não encontrada.", ErrorType.NotFound);

        if (!string.IsNullOrEmpty(name)) organization.OrganizationName = name.Trim();

        if (!string.IsNullOrEmpty(address)) organization.OrganizationAddress = address.Trim();

        organization.OrganizationDescription = string.IsNullOrEmpty(description) ? string.Empty : description.Trim();

        if (logo is { Length: > 0 })
        {
            var (key, error) = await UploadOrganizationImageAsync(logo, organizationId, "logo");
            if (error != null || key == null) return Result.Fail(error ?? "Erro no upload da imagem", ErrorType.Misc);

            organization.LogoPath = key;
        }

        if (banner is { Length: > 0 })
        {
            var (key, error) = await UploadOrganizationImageAsync(banner, organizationId, "banner");
            if (error != null || key == null)
                return Result.Fail(error ?? "Erro no upload da imagem", ErrorType.Misc);

            organization.BannerPath = key;
        }

        await context.SaveChangesAsync();

        return Result.Ok(message: "Organização atualizada com sucesso!");
    }
    
    
    public Task<Result> DeleteProjectAsync(string projectId, string userId)
    {
        throw new NotImplementedException();
    }

    
    
    public async Task<Result> AddMemberAsync(string organizationId, string memberEmail)
    {
        if (string.IsNullOrEmpty(organizationId) || string.IsNullOrEmpty(memberEmail))
        {
            return Result.Fail("Nenhuma organização e/ou user colocado!", ErrorType.MissingComponent);   
        }
        
        var organization = await GetOrganization(organizationId);
        
        if (organization == null)
        {
            return Result.Fail("Não existe a organização!", ErrorType.NotFound);
        }

        if (await OrganizationMember.IsMember(context, memberEmail, organizationId))
        {
            return Result.Fail("O membro já pertence à organização", ErrorType.Misc);
        }
        
        var user = await context.Users
            .Where(u => u.Email == memberEmail)
            .Select(u => new {u.Id, u.Email})
            .FirstOrDefaultAsync();
        
        if (user == null)
        {
            return Result.Fail("Não existe utilizador com esse email", ErrorType.Misc);
        }

        await notificationService.NotifyOrganizationInviteAsync(user.Id, organization.OrganizationId, organization.OrganizationName);
        
        return Result.Ok(message: "Membro convidado com sucesso!");
    }

    public async Task<Result> DeleteMemberAsync(string memberId, string organizationId, string userId)
    {
        if (!context.OrganizationMembers.Any(p => p.User == userId && p.Organization == organizationId && p.Role == Role.Owner))
        {
            return Result.Fail("Não tem permissão para remover membros!", ErrorType.Denied);
        }

        if (context.OrganizationMembers.Any(p => p.User == memberId && p.Organization == organizationId && p.Role == Role.Owner))
        {
            return Result.Fail("Não pode remover o dono da organização!", ErrorType.Misc);
        }
            
        context.OrganizationMembers.RemoveRange(
            context.OrganizationMembers
                .Where(p => p.User == memberId && p.Organization == organizationId)
                .ToList()
        );
            
        await context.SaveChangesAsync();
        
        return Result.Ok(message: "Membro removido com sucesso!");
    }

    public async Task<Result> PromoteMemberAsync(string memberId, string organizationId, string userId)
    {
        if (string.IsNullOrEmpty(organizationId) || string.IsNullOrEmpty(memberId) || string.IsNullOrEmpty(userId))
        {
            return Result.Fail("Falta componentes para a operação",  ErrorType.MissingComponent);
        }
        
        var permission = await MemberBelongsToOrganization(organizationId, userId);
        if (permission == null) return Result.Fail("Ocorreu um erro na autorização!",  ErrorType.Misc);
        var isOwnerOrManager = permission.Role != Role.Owner || permission.Role != Role.Manager;
        if (!isOwnerOrManager)
        {
            return Result.Fail("Não tem permissões para promover membros!", ErrorType.Misc);
        }
        var member = await MemberBelongsToOrganization(organizationId, memberId);
        if (member == null)
        {
            return Result.Fail("O membro não pertence a organização!", ErrorType.Misc);
        }
        var newRole = member.Role switch
        {
            Role.Apprentice => Role.Mentor,
            Role.Mentor when permission.Role == Role.Owner => Role.Manager, //Manager não pode promover Mentor para Manager
            Role.Mentor => Role.Mentor,
            _ => member.Role
        };
        if (newRole == member.Role)
            return Result.Fail("Este membro já está no papel máximo permitido",  ErrorType.Misc);
       
        member.Role = newRole;
        await context.SaveChangesAsync();
       
        return Result.Ok(message: $"Membro promovido a {newRole}!", newRole.ToString());
    }


    public async Task<Result> DeleteOrganizationAsync(string organizationId, string userId)
    {
        if (string.IsNullOrEmpty(organizationId) || string.IsNullOrEmpty(userId))
        {
            return Result.Fail("Nenhuma organização e/ou user colocado!", ErrorType.MissingComponent);            
        }
        
        var permission = await MemberBelongsToOrganization(organizationId, userId);
        if (permission == null || permission.Role != Role.Owner)
        {
            return Result.Fail("Apenas o dono pode eliminar a organização", ErrorType.Denied);
        }
        
        var organization = await context.Organizations
            .FirstOrDefaultAsync(o => o.OrganizationId == organizationId);
        if (organization == null)
            return Result.Fail("Organização não existente", ErrorType.NotFound);
        
        // Subqueries encadeadas — nada é trazido para memória, tudo fica em SQL
        var projectIds = context.Project.Where(p => p.OrganizationId == organizationId).Select(p => p.ProjectId);
        var fileIds = context.Files.Where(f => projectIds.Contains(f.ProjectId)).Select(f => f.FileId);
        var sessionIds = context.Sessions.Where(s => fileIds.Contains(s.fileId)).Select(s => s.Id);

        // Ordem importa: das dependências mais profundas para as mais superficiais (FK constraints)
        context.ChatMessages.RemoveRange(context.ChatMessages.Where(c => sessionIds.Contains(c.SessionId)));
        context.SessionAccesses.RemoveRange(context.SessionAccesses.Where(sa => sessionIds.Contains(sa.SessionId)));
        context.Sessions.RemoveRange(context.Sessions.Where(s => fileIds.Contains(s.fileId)));
        context.UserProjectAccesses.RemoveRange(context.UserProjectAccesses.Where(u => projectIds.Contains(u.ProjectId)));
        context.Files.RemoveRange(context.Files.Where(f => projectIds.Contains(f.ProjectId)));
        context.Project.RemoveRange(context.Project.Where(p => p.OrganizationId == organizationId));
        context.OrganizationMembers.RemoveRange(context.OrganizationMembers.Where(m => m.Organization == organizationId));
        context.Posts.RemoveRange(context.Posts.Where(p => p.OrganizationId == organizationId));
        context.Organizations.Remove(organization);

        await context.SaveChangesAsync();
        
        return Result.Ok(message: "Organização eliminada com sucesso!");
    }

    public async Task<Result> CreatePostAsync(string organizationId, string newPostTitle, string newPostContent, string userId)
    {
        if (string.IsNullOrEmpty(newPostTitle) || string.IsNullOrEmpty(newPostContent))
        {
            return Result.Fail("É obrigatório preencher os campos todos", ErrorType.MissingComponent);
        }
        
        var permission =  await MemberBelongsToOrganization(organizationId, userId);
        if (permission == null) return Result.Fail("Não tem permissão para criar publicações", ErrorType.Denied);
        
        context.Posts.Add(new Post
        {
            PostId = Guid.NewGuid().ToString(),
            Title = newPostTitle,
            Content = newPostContent,
            Created = DateTime.UtcNow,
            AuthorID = userId,
            OrganizationId = organizationId,
            Visible = true
        });
        
        await context.SaveChangesAsync();
        
        return Result.Ok(message: "Publicação feita com sucesso!");
    }

    public async Task<Result> DeletePostAsync(string postId, string userId)
    {
        if (string.IsNullOrEmpty(postId)) return Result.Fail("Nenhuma organização especificada!", ErrorType.MissingComponent);
        var post = await context.Posts.FirstOrDefaultAsync(p => p.PostId == postId);
        if (post == null) return Result.Fail("A publicação selecionada não existe!", ErrorType.Misc);
        
        context.Posts.Remove(post);
        await context.SaveChangesAsync();

        return Result.Ok(message: "Publicação removida com sucesso!");
    }


    public async Task<OrganizationMember?> MemberBelongsToOrganization(string organizationId, string userId)
    {
        return await context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.Organization == organizationId && m.User == userId);
    }
    private async Task<Organization?> GetOrganization(string organizationId)
    {
        return await context.Organizations.FirstOrDefaultAsync(o => o.OrganizationId == organizationId);
    }
    private async Task<(string? Key, string? Error)> UploadOrganizationImageAsync(IFormFile file, string organizationId, string imageType)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(ext)) return (null, $"{(imageType == "logo" ? "Logo" : "Banner")} deve ser PNG, JPG ou WebP.");

        using var memory = new MemoryStream();
        await file.CopyToAsync(memory);

        var key = $"{organizationId}_{imageType}_{Guid.NewGuid()}{ext}";
        var ok = await iS3Api.UploadBinaryAsync("logos", key, memory.ToArray(), file.ContentType);

        return ok ? (key, null) : (null, $"Falha ao carregar {imageType}.");
    }
}