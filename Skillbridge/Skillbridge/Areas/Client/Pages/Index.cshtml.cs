using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Areas.Client.Models;
using Skillbridge.Data;
using Skillbridge.Models;
using Skillbridge.Models.Client;
using Skillbridge.Models.Project;
using Skillbridge.Services;

namespace Skillbridge.Areas.Client.Pages;

/// <summary>
/// Model para a pagina
/// </summary>
/// <param name="context"></param>
/// <param name="userManager"></param>
/// <param name="s3Api"></param>
/// <param name="logger"></param>
[Authorize]
public class IndexModel(
    ApplicationDbContext context,
    UserManager<User> userManager,
    ILogger<IndexModel> logger) : PageModel
{
    /// <summary>
    /// Propriedade usada pelo @model no .cshtml.
    /// </summary>
    public IndexViewModel DashboardModel { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToPage("/Index");

        try
        {
            var sessions = await GetUserSessionsAsync(user.Id);
            var organizations = await GetUserOrganizationsAsync(user.Id);
            var projects = await GetUserProjectsAsync(user.Id);

            DashboardModel = new IndexViewModel
            {
                User = user,
                Sessions = sessions,
                ActiveSessions = sessions.Count(s => s.Active),
                Organizations = organizations,
                TotalOrganizations = organizations.Count,
                Projects = projects,
                TotalProjects = projects.Count
            };

            return Page();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao carregar o dashboard do utilizador {UserId}", user.Id);
            return RedirectToPage("/Index");
        }
    }

    /// <summary>
    /// Sessões do utilizador (com ficheiro associado), mais recentes primeiro.
    /// </summary>
    private async Task<List<Session>> GetUserSessionsAsync(string userId)
    {
        return await context.SessionAccesses
            .Where(sa => sa.UserId == userId)
            .Include(sa => sa.Session)
            .ThenInclude(s => s.file)
            .Select(sa => sa.Session)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Organizações a que o utilizador pertence.
    /// </summary>
    private async Task<List<Organization>> GetUserOrganizationsAsync(string userId)
    {
        return await context.OrganizationMembers
            .Where(om => om.User == userId && om.IdOrganization != null)
            .Join(context.Organizations,
                om => om.Organization,
                org => org.OrganizationId,
                (om, org) => org)
            .ToListAsync();
    }

    /// <summary>
    /// Projetos atribuídos ao utilizador, com a respetiva organização.
    /// </summary>
    private async Task<List<Project>> GetUserProjectsAsync(string userId)
    {
        return await context.UserProjectAccesses
            .Where(upa => upa.UserId == userId)
            .Join(context.Project,
                upa => upa.ProjectId,
                pj => pj.ProjectId,
                (upa, pj) => pj)
            .Join(context.Organizations,
                pj => pj.OrganizationId,
                org => org.OrganizationId,
                (pj, org) => new Project(pj, org))
            .ToListAsync();
    }
}