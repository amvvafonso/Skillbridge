using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Areas.Client.Models;
using Skillbridge.Data;
using Skillbridge.Models;
using Skillbridge.Models.Client;
using Skillbridge.Models.Project;
using Skillbridge.Models.Utils;


namespace Skillbridge.Areas.Client.Pages
{

    [Authorize]
    public class IndexModel(ApplicationDbContext context, UserManager<User> userManager) : PageModel
    {
        private readonly S3Api s3Api;
        //Propriedade que vai ser usada pelo @model no .cshtml
        public IndexViewModel DashboardModel { get; set; } = default!;
        
        //
        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                //Vai buscar o user
                var user = await userManager.GetUserAsync(User);


                //Se o user não estiver autenticado redireciona para Home
                if (user == null)
                    return RedirectToPage("/Index");

                //Vai buscar todas as sessões com o ficheiro associado, ordenadas por data
                var userSessions = await context.SessionAccesses
                    //So as sessões do user autenticado
                    .Where(sa => sa.UserId == user.Id)
                    //Inclui a sessão de cada acesso
                    .Include(sa => sa.Session)
                    //Inlcui o ficheiro de cada sessão
                    .ThenInclude(s => s.file)
                    //Vai buscar o objeto Session
                    .Select(sa => sa.Session)
                    //Ordena da mais recente para a mais antiga
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                //Vai buscar as organizações a que o user pertence
                var userOrganizations = await context.OrganizationMembers
                    .Where(om => om.User == user.Id)
                    .Select(om => om.IdOrganization)
                    .ToListAsync();

                //Vai buscar os projetos atribuidos ao utilizador
                var userProjects = await context.UserProjectAccesses
                    .Join(context.Project,
                        pj => pj.ProjectId,
                        usp => usp.ProjectId,
                        (usp, pj) => new { pj, usp })
                    .Join(context.Organizations,
                        prev => prev.pj.OrganizationId,
                        org => org.OrganizationId,
                        (prev, org) => new { prev.pj, prev.usp, org })
                    .Where(upa => upa.usp.UserId == user.Id)
                    .Select(upa => new Project(upa.pj, upa.org))
                    .ToListAsync();
                
                
                
                //Preenche o ViewModel com os dados recolhidos
                DashboardModel = new IndexViewModel
                {
                    User = user,
                    Sessions = userSessions,
                    ActiveSessions = userSessions.Count(s => s.Active),
                    TotalProjects = userProjects.Count,
                    TotalOrganizations = userOrganizations.Count,
                    Organizations = userOrganizations,
                    Projects = userProjects
                };

                return Page();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return RedirectToPage("/");
            }
        }
        public async Task<IActionResult> OnGetAvatarAsync(string key)
        {
            var image = await s3Api.GetBinaryAsync("logos", key);

            if (image == null)
                return NotFound();

            return File(image.Value.Data, image.Value.ContentType);
        }
    }
}