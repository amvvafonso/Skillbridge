using System.Security.Claims;
using Amazon.RuntimeDependencies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Data;
using Skillbridge.Hubs;
using Skillbridge.Models;
using Skillbridge.Models.Client;
using Skillbridge.Models.Project;
using Skillbridge.Models.Utils;
using Skillbridge.Utilities;

namespace Skillbridge.Pages.Organization;

public class ProfileModel(ApplicationDbContext context, IHubContext<NotificationHub> notificationHub, S3Api s3Api) : PageModel
{
    //Limit of posts visible
    private readonly int POST_LIMIT = 3;
    private readonly IHubContext<NotificationHub> notificationHub = notificationHub;

    private readonly S3Api s3Api = s3Api;
    // DB table vars
    public Models.Organization? Organization { get; set; }
    public List<UserInfo> Members { get; set; } = [];
    public List<ProjectInfo> Projects { get; set; } = [];
    public List<PostInfo> Posts { get; set; } = [];
    public Role? UserPerm { get; set; }
    
    // Record modifier provides built-in functionality for encapsulating data
    public record UserInfo(string UserId, string Name, string Email, Role Role);
    public record ProjectInfo(int Id, string Name, string Description, bool IsPublic);
    public record PostInfo(string Id, string Title, string Content, DateTime Created, string AuthorName);

    public record CurrentUserInfo(string Id, Role Role, string OrgId);

    public IActionResult OnGet(string id)
    {
        // Fetch organization
        Organization = context.Organizations
            .Include(o => o.OrganizationOwner)
            .FirstOrDefault(o => o.OrganizationId == id);

        if (Organization == null)
            return RedirectToPage("/");

        // Members: owner + invited members
        var members = context.OrganizationMembers
            .Where(om => om.Organization == id)
            .Select(om => new {User = om.IdUser, Role = om.Role})
            .ToList();

        // Add owner to the members
        if (Organization.OrganizationOwner != null)
            members.Add(new {User = Organization.OrganizationOwner, Role = Role.Owner});

        Members = members
                .Distinct()
                .Select(u => new UserInfo(u.User.Id ,u.User.Name ?? string.Empty, u.User.Email ?? string.Empty, u.Role))
                .ToList();
        
        // Projets that are owned by the organization that are public
        // Switches between what to show in the project section
        switch (UserPerm)
        {
            case Role.Manager or Role.Owner:
                //Shows all of them
                Projects = context.Project
                    .Where(p => p.OrganizationId == id)
                    .Select(p => new ProjectInfo(p.ProjectId, p.ProjectName ?? string.Empty, p.ProjectDescription ?? string.Empty, p.Public))
                    .ToList();
                break;
            default:
                //Only shows the public ones
                Projects = context.Project
                    .Where(p => p.OrganizationId == id && p.Public)
                    .Select(p => new ProjectInfo(p.ProjectId, p.ProjectName ?? string.Empty, p.ProjectDescription ?? string.Empty, p.Public))
                    .ToList();
                    break;
        }

        
        // Announcements by the organization, limited by POST_LIMIT
        Posts = context.Posts
            .OrderByDescending(p => p.Created)
            .Where(p => p.OrganizationId == Organization.OrganizationId)
            .Take(POST_LIMIT)
            .Join(context.Users,
                p => p.AuthorID,
                u => u.Id,
                (p, u) => new PostInfo(p.PostId, p.Title, p.Content, p.Created, u.Name ?? string.Empty))
            .ToList();
        
        // Fetch authenticade user id to compare to db
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (User.Identity is { IsAuthenticated: true })
        {
            // Information of user entering the profile
            var roleNullable = context.OrganizationMembers
                .Where(o => o.Organization == id && o.User == userId) // filtra nas propriedades reais
                .Select(p => (Role?)p.Role)
                .FirstOrDefault();
            
            UserPerm = roleNullable ?? Role.Unknown;
        }
        else
        {
            UserPerm = Role.Unknown;
        }

        return Page();
    }

    public IActionResult OnPost()
    {
        
        return Page();
    }

    [BindProperty]
    public string? MemberEmail { get; set; }

    [BindProperty]
    public string? NewPostTitle { get; set; }

    [BindProperty]
    public string? NewPostContent { get; set; }

    [BindProperty]
    public string? NewProjectName { get; set; }

    [BindProperty]
    public string? NewProjectDescription { get; set; }

    [BindProperty]
    public string? NewProjectRepository { get; set; }

    [BindProperty]
    public bool NewProjectPublic { get; set; } = true;

    public async Task<IActionResult> OnPostCreatePostAsync(string id)
    {
        try
        {
            if (string.IsNullOrEmpty(NewPostTitle) || string.IsNullOrEmpty(NewPostContent))
                return new JsonResult(new { success = false, message = "Preenche o título e o conteúdo!" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return new JsonResult(new { success = false, message = "Precisas de estar autenticado." });

            context.Posts.Add(new Post
            {
                PostId = Guid.NewGuid().ToString(),
                Title = NewPostTitle,
                Content = NewPostContent,
                Created = DateTime.UtcNow,
                AuthorID = userId,
                OrganizationId = id,
                Visible = true
            });

            await context.SaveChangesAsync();
            return new JsonResult(new { success = true, message = "Publicação criada com sucesso!" });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostDeletePostAsync([FromQuery] string postId)
    {
        try
        {
            // Verifies that the posts exists
            var post = await context.Posts.FindAsync(postId);
            
            if (post == null)
                return new JsonResult(new { success = false, message = "Publicação não encontrada." });
            
            // Removes it from db
            context.Posts.Remove(post);
            // Saves changes
            await context.SaveChangesAsync();
            // Returns json for the toast
            return new JsonResult(new { success = true, message = "Publicação removida!" });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }
    
    public async Task<IActionResult> OnPostCreateProjectAsync(string id)
    {
        try
        {
            if (string.IsNullOrEmpty(NewProjectName) || string.IsNullOrEmpty(NewProjectDescription))
                return new JsonResult(new { success = false, message = "Preenche o nome e a descrição do projeto!" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return new JsonResult(new { success = false, message = "Precisas de estar autenticado." });

            // Verifies user has Manager or Owner role in this organization
            var member = await context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.Organization == id && m.User == userId);

            if (member == null || (member.Role != Role.Owner && member.Role != Role.Manager))
                return new JsonResult(new { success = false, message = "Não tens permissão para criar projetos." });

            // Generate S3 bucket name from project name (slug)
            var slug = NewProjectName.ToLower()
                .Replace(" ", "-")
                .Replace("á", "a").Replace("à", "a").Replace("â", "a").Replace("ã", "a")
                .Replace("é", "e").Replace("ê", "e")
                .Replace("í", "i").Replace("î", "i")
                .Replace("ó", "o").Replace("ô", "o").Replace("õ", "o")
                .Replace("ú", "u").Replace("û", "u")
                .Replace("ç", "c")
                .Replace("ã", "a")
                .Replace("ó", "o")
                .Replace("ó", "o")
                .Replace("ó", "o");

            // Remove consecutive dashes and trim
            while (slug.Contains("--")) slug = slug.Replace("--", "-");
            slug = slug.Trim('-');
            
            var newProject = new Models.Project.Project
            {
                ProjectName = NewProjectName,
                ProjectDescription = NewProjectDescription,
                Repository = NewProjectRepository,
                Public = NewProjectPublic,
                ProjectDirectory = slug,
                OrganizationId = id
            };
            
            context.Project.Add(newProject);
            await context.SaveChangesAsync();
            
            // Gives permission to all user in the organization
            context.UserProjectAccesses.AddRange(
                context.OrganizationMembers
                    .Where(m => m.Organization == id)
                    .Select(p => new UserProjectAccess(Role.Apprentice, p.User, newProject.ProjectId))
                    .ToList()
                );
            
            await context.SaveChangesAsync();
            // Try to create S3 bucket for the project
            try
            {
                await s3Api.CriarBucketAsync(NewProjectName);
            }
            catch
            {
                // S3 bucket creation is not critical - project still created in DB
            }

            return new JsonResult(new { success = true, message = "Projeto criado com sucesso!" });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostAddMemberAsync(string Id)
    {
        try
        {
            // Verifies that the email was provied
            if (string.IsNullOrEmpty(MemberEmail))
            {
                return new JsonResult(new { success = false, message = "Por favor, indique um email para convidar!" });
            }
        
            // Verifies that there is an organization selected
            Organization = await context.Organizations
                .FirstOrDefaultAsync(o => o.OrganizationId == Id);

            if (Organization == null)
            {
                return new JsonResult(new { success = false, message = "Organização não existe ou ocorreu algum erro!" });

            }
            
            
            // Verifies if the email provided is already a member
            if (await OrganizationMember.IsMember(context, MemberEmail, Organization.OrganizationId))
            {
                return  new JsonResult(new { success = false, message = "Esse utilizador já pertence à organização!" });
            }
            
            
            // Verifies that the user exists and gets his user id for the notification
            var userExists = await context.Users
                .Where(u => u.Email == MemberEmail)
                .Select(u => new {u.Id, u.Email})
                .FirstOrDefaultAsync();
            
            if (userExists != null)
            {
                // Creates a new NotificationParam i.e. param is the important field of the notification
                NotificationParam np = new NotificationParam();
                // In this case, param is the ID of the organization since is an organization invite
                np.Param = Id;
                // 'Other' is a redundancy to deal with specific cases 
                np.Other = new Dictionary<string, string>{{"message", $"Foste convidado para a organização {Organization?.OrganizationName}"}};

                // Creates the notification in the DB
                context.Notifications.Add(new Notification(np, userExists.Id, NotificationType.OrganizationInvite));
                await context.SaveChangesAsync();
                
                // Sends notification for the user invited
                notificationHub.Clients.User(userExists.Id).SendAsync("ReceiveNotification", $"Foste convidado para a organização {Organization?.OrganizationName}");
                //Toast
                return new JsonResult(new { success = true, message = "Convite enviado com sucesso!" });
            }

            return new JsonResult(new { success = false, message = "Utilizador não encontrado." });


        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }
    
    public async Task<IActionResult> OnGetImageAsync(string key)
    {
        Console.WriteLine(key);
        if (string.IsNullOrWhiteSpace(key))
            return NotFound();

        var result = await s3Api.GetBinaryAsync("logos", key);

        if (result == null)
            return NotFound();

        return File(result.Value.Data, result.Value.ContentType);
    }


    public async Task<IActionResult> OnPostDeleteOrganizationAsync([FromForm] string organizationId)
    {
        try
        {
            // Verifies the user is logged
            if (!User.Identity.IsAuthenticated)
            {
                return Forbid();
            }
            var user = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            // Verifies the user has permission
            if (!context.OrganizationMembers.Any(p => p.User == user && p.Organization == organizationId && p.Role == Role.Owner))
            {
                return Forbid();
            }
            
            // Verifies that there is a organization with this id
            if (!context.Organizations.Any(o => o.OrganizationId == organizationId))
            {
                return BadRequest();
            }
            
            List<int> listOfProject = new List<int>();
            List<string> listOfSessions = new List<string>();
            List<string> listOfFiles = new List<string>();

            // First we need to the delete all data related to the project
            var _projects = context.Project
                .Where(p => p.OrganizationId == organizationId)
                .Select(p => p.ProjectId)
                .ToList();
            
            listOfProject.AddRange(_projects);

            foreach (var proj in listOfProject)
            {
                
                var files = context.Files
                    .Where(p => p.ProjectId == proj)
                    .Select(p => p.FileId)
                    .ToList();
                
                listOfFiles.AddRange(files) ;

            }

            foreach (var file in listOfFiles)
            {
                var sessions = context.Sessions
                    .Where(p => p.fileId == file)
                    .Select(p => p.Id)
                    .ToList();
                
                listOfSessions.AddRange(sessions);
            }
            
            // Deletion of chat messages
            foreach (var session in listOfSessions)
            {
                context.ChatMessages.RemoveRange(
                    context.ChatMessages
                        .Where(p => p.SessionId == session)
                        .ToList()
                );

                context.SessionAccesses.RemoveRange(
                    context.SessionAccesses
                        .Where(p => p.SessionId == session)
                        .ToList()
                );
            }
            
            // Delete of sessions
            foreach (var file in listOfFiles)
            {
                context.Sessions.RemoveRange(
                    context.Sessions
                        .Where(p => p.fileId == file)
                        .ToList()
                );
            }
            
            // Delete files and userproject acesses
            foreach (var project in listOfProject)
            {
                context.Files.RemoveRange(
                    context.Files
                        .Where(p => p.ProjectId == project)
                        .ToList()
                );

                context.UserProjectAccesses.RemoveRange(
                    context.UserProjectAccesses
                        .Where(p => p.ProjectId == project)
                        .ToList()
                );

            }

            // OrganizationMembers deletion
            context.OrganizationMembers.RemoveRange(
                context.OrganizationMembers
                    .Where(p => p.Organization == organizationId)
                    .ToList()
            );
            
            // Projects deletion
            context.Project.RemoveRange(
                context.Project
                    .Where(p => p.OrganizationId == organizationId)
                    .ToList()
            );
            
            // Posts deletion
            context.Posts.RemoveRange(
                context.Posts
                    .Where(p => p.OrganizationId == organizationId)
                    .ToList()
            );

            // AND AFTER 1 MILION YEARS
            var organization = await context.Organizations
                .FirstOrDefaultAsync(o => o.OrganizationId == organizationId);

            if (organization == null)
                return BadRequest();

            context.Organizations.Remove(organization);

            await context.SaveChangesAsync();
            
            return RedirectToPage("./Index");
        }
        catch (Exception es)
        {
            Console.WriteLine(es.Message);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostRemoveMemberAsync([FromForm] string memberId, [FromForm] string organizationId)
    {
        try
        {
            var user =  User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (user == null)
            {
                return new JsonResult(new { success = false, message = "Não tem permissão para remover membros!" });
            }

            if (!context.OrganizationMembers.Any(p => p.User == user && p.Organization == organizationId && p.Role == Role.Owner))
            {
                return new JsonResult(new { success = false, message = "Não tem permissão para remover membros!!" });
            }

            if (context.OrganizationMembers.Any(p => p.User == memberId && p.Organization == organizationId && p.Role == Role.Owner))
            {
                return new JsonResult(new { success = false, message = "Não pode remover o dono da organização!" });
            }
            
            context.OrganizationMembers.RemoveRange(
                context.OrganizationMembers
                    .Where(p => p.User == memberId && p.Organization == organizationId)
                    .ToList()
                );
            
            await context.SaveChangesAsync();
            
            return new JsonResult(new { success = true, message = "Membro removido com sucesso da organização" });
        }
        catch (Exception es)
        {
            Console.WriteLine(es.Message);
            return new JsonResult(new { success = false, message = "Ocorreu um erro na operação" });
        }
    }

    public async Task<IActionResult> OnPostUpgradeMemberAsync(string organizationId, string memberId)
    {
        try
        {
            //Obtem o ID do user autenticado
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //Verifica se o utilizador pertence à organização
            var currentMember = await context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.Organization == organizationId && m.User == userId);

            //Bloqueia se não pertencer ou não tiver permissão de Manager/Owner
            if (currentMember == null || (currentMember.Role != Role.Owner && currentMember.Role != Role.Manager))
                return new JsonResult(new { success = false, message = "Não tens permissão para fazer isto" });

            //Vai buscar o membro que vai ser promovido
            var member = await context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.Organization == organizationId && m.User == memberId);

            //Verifica se o membro existe
            if (member == null)
                return new JsonResult(new { success = false, message = "Membro não encontrado" });

            //Define o novo role consoanete o role atual e quem está a promover
            var newRole = member.Role switch
            {
                Role.Apprentice => Role.Mentor,
                Role.Mentor when currentMember.Role == Role.Owner => Role.Manager,
                //Manager não pode promover Mentor para Managaer
                Role.Mentor => Role.Mentor,
                _ => member.Role
            };

            //Se o role não mudou
            if (newRole == member.Role)
                return new JsonResult(
                    new { success = false, message = "Este membro já está no papel máximo permitido" });

            //Aplica o novo role e guarda
            member.Role = newRole;
            await context.SaveChangesAsync();

            return new JsonResult(new
                { success = true, message = $"Membro promovido a {newRole}!", newRole = newRole.ToString() });
        }
        catch (Exception e)
        {
            return new JsonResult(new { success = false, message = e.Message });
        }
    }

    public async Task<IActionResult> OnPostEditOrganizationAsync(
        [FromForm] string id,
        [FromForm] string EditName,
        [FromForm] string EditAddress,
        [FromForm] string EditDescription,
        [FromForm] IFormFile? EditLogo,
        [FromForm] IFormFile? EditBanner)
    {
        try
        {
            // Verify user is authenticated
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return new JsonResult(new { success = false, message = "Precisas de estar autenticado." });

            // Verify user is Owner of this organization
            var member = await context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.Organization == id && m.User == userId);

            if (member == null || member.Role != Role.Owner)
                return new JsonResult(new { success = false, message = "Apenas o dono pode editar a organização." });

            // Fetch organization
            var org = await context.Organizations.FirstOrDefaultAsync(o => o.OrganizationId == id);
            if (org == null)
                return new JsonResult(new { success = false, message = "Organização não encontrada." });

            // Update text fields
            if (!string.IsNullOrEmpty(EditName))
                org.OrganizationName = EditName.Trim();
            if (!string.IsNullOrEmpty(EditAddress))
                org.OrganizationAddress = EditAddress.Trim();
            org.OrganizationDescription = string.IsNullOrEmpty(EditDescription) ? null : EditDescription.Trim();

            // Upload new logo if provided
            if (EditLogo != null && EditLogo.Length > 0)
            {
                var ext = Path.GetExtension(EditLogo.FileName).ToLowerInvariant();
                if (ext is not ".png" and not ".jpg" and not ".jpeg" and not ".webp")
                    return new JsonResult(new { success = false, message = "Logo deve ser PNG, JPG ou WebP." });

                using var logoStream = EditLogo.OpenReadStream();
                var logoData = new byte[EditLogo.Length];
                await logoStream.ReadAsync(logoData);

                var key = $"{id}_logo_{Guid.NewGuid()}{ext}";
                var ok = await s3Api.UploadBinaryAsync("logos", key, logoData, EditLogo.ContentType);

                if (ok)
                    org.LogoPath = key;
            }

            // Upload new banner if provided
            if (EditBanner != null && EditBanner.Length > 0)
            {
                var ext = Path.GetExtension(EditBanner.FileName).ToLowerInvariant();
                if (ext is not ".png" and not ".jpg" and not ".jpeg" and not ".webp")
                    return new JsonResult(new { success = false, message = "Banner deve ser PNG, JPG ou WebP." });

                using var bannerStream = EditBanner.OpenReadStream();
                var bannerData = new byte[EditBanner.Length];
                await bannerStream.ReadAsync(bannerData);

                var key = $"{id}_banner_{Guid.NewGuid()}{ext}";
                var ok = await s3Api.UploadBinaryAsync("logos", key, bannerData, EditBanner.ContentType);

                if (ok)
                    org.BannerPath = key;
            }

            await context.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "Organização atualizada com sucesso!" });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }
}
