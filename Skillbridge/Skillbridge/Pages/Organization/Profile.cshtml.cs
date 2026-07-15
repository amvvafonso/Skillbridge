using System.Security.Claims;
using Amazon.RuntimeDependencies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Skillbridge.Data;
using Skillbridge.Hubs;
using Skillbridge.Models;
using Skillbridge.Models.Client;
using Skillbridge.Models.Project;
using Skillbridge.Services;
using Skillbridge.Utilities;

namespace Skillbridge.Pages.Organization;

public class ProfileModel(ApplicationDbContext context, IOrganizationService organizationService, IProjectService projectService, IOrganizationMemberService organizationMemberService) : PageModel
{
    //Limit of posts visible
    private const int PostLimit = 3;

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
            .Take(PostLimit)
            .Join(context.Users,
                p => p.AuthorID,
                u => u.Id,
                (p, u) => new PostInfo(p.PostId, p.Title, p.Content, p.Created, u.Name ?? string.Empty))
            .ToList();
        

        return Page();
    }

    public IActionResult OnPost()
    {
        
        return Page();
    }
    
    
    
    //Done
    public async Task<IActionResult> OnPostCreatePostAsync(string id, [FromForm] string newPostTitle, [FromForm] string newPostContent)
    {

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return new JsonResult(new { success = false, message = "Precisas de estar autenticado." });
        var result =  await organizationService.CreatePostAsync(id, newPostTitle, newPostContent, userId);
        switch (result.ErrorType)
        {
            case  ErrorType.Denied: return Forbid();
            case  ErrorType.NotFound: return NotFound();
        }
        return new JsonResult(new { success = result.Success, message = result.Message });
    }
    //Done
    public async Task<IActionResult> OnPostDeletePostAsync([FromQuery] string postId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return new JsonResult(new {success = false, message = "Precisas de estar autenticado!" });
            
        var result = await organizationService.DeletePostAsync(postId, userId);
        switch (result.ErrorType)
        {
            case  ErrorType.Denied: return Forbid();
            case  ErrorType.NotFound: return NotFound();
        }
            
        return new JsonResult(new { success = result.Success, message = result.Message });
    }
    
    

    //Done
    // Required for checkbox behavior
    [BindProperty]
    public bool NewProjectPublic { get; set; }
    public async Task<IActionResult> OnPostCreateProjectAsync(string id, [FromForm] string newProjectName,  [FromForm] string newProjectDescription, [FromForm] string newProjectRepository)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return new JsonResult(new { success = false, message = "Precisas de estar autenticado." });
        
        var result = await projectService.CreateProjectAsync(
            id, userId, newProjectName, newProjectDescription, newProjectRepository, NewProjectPublic);

        switch (result.ErrorType)
        {
            case  ErrorType.Denied:
                return Forbid();
            case  ErrorType.NotFound:
                return NotFound();
        }
        
        return new JsonResult(new { success = result.Success, message = result.Message });
    }

    
    //Done
    public async Task<IActionResult> OnPostAddMemberAsync(string id, [FromForm] string memberEmail)
    {
        var  userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return new JsonResult(new { success = false, message = "É preciso estar autenticado para esta operação!" });
        
        var result = await organizationService.AddMemberAsync(id, memberEmail, userId);

        switch (result.ErrorType)
        {
            case  ErrorType.Denied: return Forbid();
            case  ErrorType.NotFound: return NotFound();
        }
        return new JsonResult(new { success = result.Success, message = result.Message });
    }
    
    //Done
    public async Task<IActionResult> OnPostDeleteOrganizationAsync([FromForm] string organizationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Forbid();
        
        var result = await organizationService.DeleteOrganizationAsync(organizationId, userId);
        switch (result.ErrorType)
        {
            case ErrorType.Denied: return  Forbid();
            case ErrorType.NotFound: return NotFound();
        }
        
        return LocalRedirect("/Organization/Index");
    }

    //Done 
    public async Task<IActionResult> OnPostRemoveMemberAsync([FromForm] string memberId, [FromForm] string organizationId)
    {
        var user =  User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (user == null) return new JsonResult(new { success = false, message = "Não tem permissão para remover membros!" });
            
        var result = await organizationService.DeleteMemberAsync(memberId ,organizationId, user);
        
        switch (result.ErrorType)
        {
            case  ErrorType.Denied: return Forbid();
            case  ErrorType.NotFound: return NotFound();
        }    
        
        return new JsonResult(new { success = result.Success, message = result.Message });
    }

    // Done
    public async Task<IActionResult> OnPostUpgradeMemberAsync(string organizationId, string memberId)
    {
        //Obtem o ID do user autenticado
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if(string.IsNullOrEmpty(userId)) return new JsonResult(new { success = false, message = "É preciso estar autenticado"});
        var result = await organizationService.PromoteMemberAsync(memberId, organizationId, userId);
        
        switch (result.ErrorType)
        {
            case  ErrorType.Denied: return Forbid();
            case  ErrorType.NotFound: return NotFound();
        }
        
        
        return new JsonResult(new { success = result.Success, message = result.Message, newRole = result.Additional });
    }
    

    //Done
    public async Task<IActionResult> OnPostEditOrganizationAsync(
        [FromForm] string organizationId,
        [FromForm] string editName,
        [FromForm] string editAddress,
        [FromForm] string editDescription,
        [FromForm] IFormFile? editLogo,
        [FromForm] IFormFile? editBanner)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return new JsonResult(new { success = false, message = "Precisas de estar autenticado." });

        var result = await organizationService.EditOrganizationAsync(
            organizationId, userId, editName, editAddress, editDescription, editLogo, editBanner);

        if (result.ErrorType == ErrorType.Denied) return Forbid();
        if (result.ErrorType == ErrorType.NotFound) return NotFound();

        return new JsonResult(new { success = result.Success, message = result.Message });
    }
    
}
