using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Data;
using Skillbridge.Models;

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
    
    // Record modifier provides built-in functionality for encapsulating data
    public record UserInfo(string Name, string Email);
    public record ProjectInfo(int Id, string Name, string Description, bool IsPublic);
    public record PostInfo(string Id, string Title, string Content, DateTime Created, string AuthorName);

    

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
            .Select(om => om.IdUser)
            .ToList();
        
        // Add owner to the members
        if (Organization.OrganizationOwner != null)
            members.Add(Organization.OrganizationOwner);
        
        Members = members
            .Distinct()
            .Select(u => new UserInfo(u.Name ?? string.Empty, u.Email ?? string.Empty))
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
        return Page();
    }
}
