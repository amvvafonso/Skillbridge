using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Data;
using Skillbridge.Models;
using Skillbridge.Models.Client;
using Skillbridge.Utilities;

namespace Skillbridge.Pages.Organization;

public class ProfileModel(ApplicationDbContext context) : PageModel
{
    //Limit of posts visible
    private readonly int POST_LIMIT = 3;
    
    // DB table vars
    public Models.Organization? Organization { get; set; }
    public List<UserInfo> Members { get; set; } = [];
    public List<ProjectInfo> Projects { get; set; } = [];
    public List<PostInfo> Posts { get; set; } = [];
    public Role? UserPerm { get; set; }
    
    // Record modifier provides built-in functionality for encapsulating data
    public record UserInfo(string Name, string Email, Role Role);
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
            return NotFound();

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
                .Select(u => new UserInfo(u.User.Name ?? string.Empty, u.User.Email ?? string.Empty, u.Role))
                .ToList();
        
        // Projets that are owned by the organization that are public
        Projects = context.Project
            .Where(p => p.OrganizationId == id && p.Public)
            .Select(p => new ProjectInfo(p.ProjectId, p.ProjectName ?? string.Empty, p.ProjectDescription ?? string.Empty, p.Public))
            .ToList();
        
        // Announcements by the organization, limited by POST_LIMIT
        Posts = context.Posts
            .Where(p => p.Organization == Organization.OrganizationId)
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
    public async Task<IActionResult> OnPostAddMemberAsync(string Id)
    {
        try
        {
            // Verifies that the email was provied
            if (string.IsNullOrEmpty(MemberEmail))
            {
                return new JsonResult(new { success = false, message = "Por favor, indique um email para convidar!" });
            }
        
            // Verifies that there is a organization selected
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
    
}
