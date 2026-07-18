using Microsoft.EntityFrameworkCore;
using Skillbridge.Data;
using Skillbridge.Models;
using Skillbridge.Models.Client;
using Skillbridge.Utilities;

namespace Skillbridge.Services;


/// <summary>
/// Serviço responsável pela gestão de organizações, incluindo criação, edição,
/// remoção, gestão de membros e publicações associadas
/// </summary>
public interface IOrganizationService
{
    /// <summary>
    /// Cria uma nova organização e define o utilizador criador como Owner
    /// </summary>
    /// <param name="userId">Identificador do user que cria a organização</param>
    /// <param name="organizationName">Nome da organização</param>
    /// <param name="organizationAddress">Endereço da organização</param>
    /// <param name="organizationDescription">Descrição da organização</param>
    /// <param name="logo">Ficheiro de imagem opcional para o logotipo</param>
    /// <returns>Um <see cref="Result"/> indicando sucesso ou falha da operação</returns>
    Task<Result> CreateOrganizationAsync(string userId, string organizationName, string organizationAddress, string organizationDescription, IFormFile? logo);
    
    /// <summary>
    /// Edita os dados de uma organização existente. Apenas o Owner pode realizar esta operação
    /// </summary>
    /// <param name="organizationId">Identificador da organização a editar</param>
    /// <param name="userId">Identificador do user que solicita a edição</param>
    /// <param name="name">Novo nome da organização se fornecido</param>
    /// <param name="address">Novo endereço da organização se fornecido</param>
    /// <param name="description">Nova descrição da organização</param>
    /// <param name="logo">Novo logotipo se fornecido</param>
    /// <param name="banner">Novo banner se fornecido</param>
    /// <returns>Um <see cref="Result"/> indicando sucesso ou falha da operação</returns>
    Task<Result> EditOrganizationAsync(string organizationId, string userId, string? name, string? address,  string? description, IFormFile? logo, IFormFile? banner);
    
    /// <summary>
    /// Elimina uma organização e todos os dados associados (projetos, ficheiros, sessões, publicações e membros) em cascata
    /// Apenas o Owner pode realizar esta operação este ação é irreversível
    /// </summary>
    /// <param name="orgId">Identificador da organização a eliminar</param>
    /// <param name="userId">Identificador do user que solicita a eliminação</param>
    /// <returns>Um <see cref="Result"/> indicando sucesso ou falha da operação</returns>
    Task<Result> DeleteOrganizationAsync(string orgId, string userId);
    
    /// <summary>
    /// Elimina um projeto, este método ainda não implementado, lança <see cref="NotImplementedException"/>.
    /// </summary>
    /// <param name="projectId">Identificador do projeto a eliminar</param>
    /// <param name="userId">Identificador do user que solicita a eliminação</param>
    Task<Result> DeleteProjectAsync(string projectId, string userId);
    
    /// <summary>
    /// Convida um novo membro para a organização através do email, apenas Owner ou Manager podem convidar
    /// </summary>
    /// <param name="organizationId">Identificador da organização</param>
    /// <param name="memberEmail">Email do utilizador a convidar</param>
    /// <param name="userId">Identificador do user que envia o convite</param>
    /// <returns>Um <see cref="Result"/> indicando sucesso ou falha da operação</returns>
    Task<Result> AddMemberAsync(string organizationId, string memberEmail, string userId);
    
    /// <summary>
    /// Remove um membro da organização, apenas o Owner pode remover membros e o mesmo não pode ser removido
    /// </summary>
    /// <param name="memberId">Identificador do membro a remover</param>
    /// <param name="organizationId">Identificador da organização</param>
    /// <param name="userId">Identificador do user que solicita a remoção</param>
    /// <returns>Um <see cref="Result"/> indicando sucesso ou falha da operação</returns>
    Task<Result> DeleteMemberAsync(string memberId, string organizationId,  string userId);
    
    /// <summary>
    /// Promove um membro para o papel seguinte na hierarquia da organização, apenas Owner ou Manager podem promover, e apenas o Owner pode promover Mentor a Manager
    /// </summary>
    /// <param name="memberId">Identificador do membro a promover</param>
    /// <param name="organizationId">Identificador da organização</param>
    /// <param name="userId">Identificador do user que solicita a promoção</param>
    /// <returns>Um <see cref="Result"/> indicando sucesso ou falha, incluindo o novo papel do membro</returns>
    Task<Result> PromoteMemberAsync(string memberId, string organizationId, string userId);
    
    /// <summary>
    /// Cria uma nova publicação na organização, requer que o utilizador seja membro da organização
    /// </summary>
    /// <param name="organizationId">Identificador da organização</param>
    /// <param name="newPostTitle">Título da publicação</param>
    /// <param name="newPostContent">Conteúdo da publicação</param>
    /// <param name="userId">Identificador do utilizuserador autor da publicação</param>
    /// <returns>Um <see cref="Result"/> indicando sucesso ou falha da operação</returns>
    Task<Result> CreatePostAsync(string organizationId, string newPostTitle, string newPostContent, string userId);
    
    /// <summary>
    /// Elimina uma publicação, apenas o autor ou um Owner/Manager da organização pode eliminar
    /// </summary>
    /// <param name="postId">Identificador da publicação a eliminar</param>
    /// <param name="userId">Identificador do user que solicita a eliminação</param>
    /// <returns>Um <see cref="Result"/> indicando sucesso ou falha da operação</returns>
    Task<Result> DeletePostAsync(string postId, string userId);
    
    /// <summary>
    /// Verifica se um utilizador pertence a uma determinada organização e devolve o seu registo de membro
    /// </summary>
    /// <param name="orgId">Identificador da organização</param>
    /// <param name="userId">Identificador do user</param>
    /// <returns>O <see cref="OrganizationMember"/> correspondente, ou <c>null</c> se não pertencer</returns>
    Task<OrganizationMember?> MemberBelongsToOrganization(string orgId, string userId);
}

/// <inheritdoc />
public class OrganizationService(ApplicationDbContext context, IS3Api iS3Api, INotificationService notificationService, ILogger<OrganizationService> logger) : IOrganizationService
{
    /// <inheritdoc />
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
            var uploaded = await iS3Api.UploadBinaryAsync("logos",  $"{newGuid}{ext}" , bytes,  logo.ContentType);
            if (uploaded) logoPath = $"{newGuid}{ext}";
        }

        var organization = new Organization
        {
            OrganizationId = newGuid,
            OrganizationName = organizationName,
            OrganizationAddress = organizationAddress,
            Owner = userId,
            LogoPath = logoPath,
            BannerPath = "default_banner.png"
        };
        context.Organizations.Add(organization);
        context.OrganizationMembers.Add(new OrganizationMember(Guid.NewGuid().ToString(), newGuid, userId, Role.Owner));
        await context.SaveChangesAsync();

        return Result.Ok(message: "Organização criada com sucesso!");
    }
    
    private static readonly string[] AllowedImageExtensions = [".png", ".jpg", ".jpeg", ".webp"];

    /// <inheritdoc />
    public async Task<Result> EditOrganizationAsync(string organizationId, string userId, string? name, string? address, string? description, IFormFile? logo, IFormFile? banner)
    {
        var permission = await MemberBelongsToOrganization(organizationId, userId);
        if (permission == null || permission.Role != Role.Owner)
        {
            logger.LogWarning("Utilizador {UserId} tentou editar a organização {OrganizationId} sem ser dono", userId, organizationId);
            return Result.Fail("Apenas o dono pode editar a organização.", ErrorType.Denied);
        }

        var organization = await GetOrganization(organizationId);
        if (organization == null) return Result.Fail("Organização não encontrada.", ErrorType.NotFound);

        if (!string.IsNullOrEmpty(name)) organization.OrganizationName = name.Trim();

        if (!string.IsNullOrEmpty(address)) organization.OrganizationAddress = address.Trim();

        organization.OrganizationDescription = string.IsNullOrEmpty(description) ? string.Empty : description.Trim();

        if (logo is { Length: > 0 })
        {
            var (key, error) = await UploadOrganizationImageAsync(logo, organizationId, "logo");
            if (error != null || key == null) return Result.Fail(error ?? "Erro no upload da imagem");

            organization.LogoPath = key;
        }

        if (banner is { Length: > 0 })
        {
            var (key, error) = await UploadOrganizationImageAsync(banner, organizationId, "banner");
            if (error != null || key == null)
                return Result.Fail(error ?? "Erro no upload da imagem");

            organization.BannerPath = key;
        }

        await context.SaveChangesAsync();

        return Result.Ok(message: "Organização atualizada com sucesso!");
    }
    
    
    public Task<Result> DeleteProjectAsync(string projectId, string userId)
    {
        throw new NotImplementedException();
    }


    /// <inheritdoc />
    public async Task<Result> AddMemberAsync(string organizationId, string memberEmail, string userId)
    {
        if (string.IsNullOrEmpty(organizationId) || string.IsNullOrEmpty(memberEmail))
            return Result.Fail("Nenhuma organização e/ou user colocado!", ErrorType.MissingComponent);

        var requester = await MemberBelongsToOrganization(organizationId, userId);
        if (requester == null || (requester.Role != Role.Owner && requester.Role != Role.Manager))
        {
            logger.LogWarning("Utilizador {UserId} tentou convidar {Email} para a organização {OrganizationId} sem permissão", userId, memberEmail, organizationId);
            return Result.Fail("Não tem permissão para convidar membros!", ErrorType.Denied);
        }

        var organization = await GetOrganization(organizationId);
        if (organization == null) return Result.Fail("Não existe a organização!", ErrorType.NotFound);

        if (await OrganizationMember.IsMember(context, memberEmail, organizationId))
            return Result.Fail("O membro já pertence à organização");
        
        var user = await context.Users
            .Where(u => u.Email == memberEmail)
            .Select(u => new {u.Id, u.Email})
            .FirstOrDefaultAsync();
        
        if (user == null) return Result.Fail("Não existe utilizador com esse email");

        await notificationService.NotifyOrganizationInviteAsync(user.Id, organization.OrganizationId, organization.OrganizationName);
        
        return Result.Ok(message: "Membro convidado com sucesso!");
    }

    /// <inheritdoc />
    public async Task<Result> DeleteMemberAsync(string memberId, string organizationId, string userId)
    {
        if (!context.OrganizationMembers.Any(p => p.User == userId && p.Organization == organizationId && p.Role == Role.Owner))
        {
            logger.LogWarning("Utilizador {UserId} tentou remover um membro da organização {OrganizationId} sem ser dono", userId, organizationId);
            return Result.Fail("Não tem permissão para remover membros!", ErrorType.Denied);
        }

        if (context.OrganizationMembers.Any(p => p.User == memberId && p.Organization == organizationId && p.Role == Role.Owner))
            return Result.Fail("Não pode remover o dono da organização!");
            
        context.OrganizationMembers.RemoveRange(
            context.OrganizationMembers
                .Where(p => p.User == memberId && p.Organization == organizationId)
                .ToList()
        );
            
        await context.SaveChangesAsync();
        
        return Result.Ok(message: "Membro removido com sucesso!");
    }

    /// <inheritdoc />
    public async Task<Result> PromoteMemberAsync(string memberId, string organizationId, string userId)
    {
        if (string.IsNullOrEmpty(organizationId) || string.IsNullOrEmpty(memberId) || string.IsNullOrEmpty(userId))
            return Result.Fail("Falta componentes para a operação",  ErrorType.MissingComponent);
        
        var permission = await MemberBelongsToOrganization(organizationId, userId);
        if (permission == null) return Result.Fail("Ocorreu um erro na autorização!");

        var isOwnerOrManager = permission.Role == Role.Owner || permission.Role == Role.Manager;
        if (!isOwnerOrManager)
        {
            logger.LogWarning("Utilizador {UserId} tentou promover um membro na organização {OrganizationId} sem ser Owner/Manager", userId, organizationId);
            return Result.Fail("Não tem permissões para promover membros!", ErrorType.Denied);
        }

        var member = await MemberBelongsToOrganization(organizationId, memberId);
        if (member == null) return Result.Fail("O membro não pertence a organização!");

        var newRole = member.Role switch
        {
            Role.Apprentice => Role.Mentor,
            Role.Mentor when permission.Role == Role.Owner => Role.Manager, //Manager não pode promover Mentor para Manager
            Role.Mentor => Role.Mentor,
            _ => member.Role
        };
        if (newRole == member.Role)
            return Result.Fail("Este membro já está no papel máximo permitido");
       
        member.Role = newRole;
        await context.SaveChangesAsync();
       
        return Result.Ok(message: $"Membro promovido a {newRole}!", newRole.ToString());
    }


    /// <inheritdoc />
    public async Task<Result> DeleteOrganizationAsync(string organizationId, string userId)
    {
        if (string.IsNullOrEmpty(organizationId) || string.IsNullOrEmpty(userId))
            return Result.Fail("Nenhuma organização e/ou user colocado!", ErrorType.MissingComponent);
        
        var permission = await MemberBelongsToOrganization(organizationId, userId);
        if (permission == null || permission.Role != Role.Owner)
        {
            logger.LogWarning("Utilizador {UserId} tentou eliminar a organização {OrganizationId} sem ser dono", userId, organizationId);
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

        // Log informativo — não é uma tentativa negada, mas vale a pena ter rasto de eliminações (ação destrutiva e irreversível)
        logger.LogInformation("Organização {OrganizationId} eliminada pelo dono {UserId}", organizationId, userId);

        return Result.Ok(message: "Organização eliminada com sucesso!");
    }

    /// <inheritdoc />
    public async Task<Result> CreatePostAsync(string organizationId, string newPostTitle, string newPostContent, string userId)
    {
        if (string.IsNullOrEmpty(newPostTitle) || string.IsNullOrEmpty(newPostContent))
            return Result.Fail("É obrigatório preencher os campos todos", ErrorType.MissingComponent);
        
        var permission =  await MemberBelongsToOrganization(organizationId, userId);
        if (permission == null)
        {
            logger.LogWarning("Utilizador {UserId} tentou criar uma publicação na organização {OrganizationId} sem pertencer a ela", userId, organizationId);
            return Result.Fail("Não tem permissão para criar publicações", ErrorType.Denied);
        }
        
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

    /// <inheritdoc />
    public async Task<Result> DeletePostAsync(string postId, string userId)
    {
        if (string.IsNullOrEmpty(postId)) return Result.Fail("Nenhuma publicação especificada!", ErrorType.MissingComponent);

        var post = await context.Posts.FirstOrDefaultAsync(p => p.PostId == postId);
        if (post == null) return Result.Fail("A publicação selecionada não existe!");

        // Só o autor ou um Owner/Manager da organização pode apagar
        var isAuthor = post.AuthorID == userId;
        var permission = await MemberBelongsToOrganization(post.OrganizationId, userId);
        var isOwnerOrManager = permission != null && (permission.Role == Role.Owner || permission.Role == Role.Manager);

        if (!isAuthor && !isOwnerOrManager)
        {
            logger.LogWarning("Utilizador {UserId} tentou apagar a publicação {PostId} sem ser autor nem Owner/Manager", userId, postId);
            return Result.Fail("Não tem permissão para apagar esta publicação!", ErrorType.Denied);
        }

        context.Posts.Remove(post);
        await context.SaveChangesAsync();

        return Result.Ok(message: "Publicação removida com sucesso!");
    }


    /// <inheritdoc />
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