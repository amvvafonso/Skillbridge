using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Areas.Client.Models;
using Skillbridge.Data;
using Skillbridge.Models;
using Skillbridge.Models.Client;

namespace Skillbridge.Areas.Client.Controllers
{
    [Area("Client")]
    [Authorize]
    public class IndexModel(ApplicationDbContext context, UserManager<User> userManager) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var user = await userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Index", "Home");

            var userSessions = context.Sessions
                .Include(s => s.file)
                .OrderByDescending(s => s.CreatedAt)
                .ToList();

            var userOrganizations = context.OrganizationMembers
                .Where(om => om.User == user.Id)
                .Select(om => om.IdOrganization)
                .ToList();

            var userProjects = context.UserProjectAccesses
                .Where(upa => upa.UserId == user.Id)
                .Select(upa => upa.Project)
                .ToList();

            var activeSessions = userSessions.Count(s => s.Active);
            var totalProjects = userProjects.Count;
            var totalOrgs = userOrganizations.Count;

            var model = new IndexViewModel
            {
                User = user,
                Sessions = userSessions,
                ActiveSessions = activeSessions,
                TotalProjects = totalProjects,
                TotalOrganizations = totalOrgs,
                Organizations = userOrganizations,
                Projects = userProjects
            };

            return View(model);
        }
    }
}