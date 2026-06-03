using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Skillbridge.Areas.Client.Models;
using Skillbridge.Data;
using Skillbridge.Models.Client;

namespace Skillbridge.Areas.Client.Controllers
{
    [Area("Client")]
    [Authorize]
    // 1. Injetas tudo diretamente aqui no Primary Constructor
    public class ClientController(ApplicationDbContext context, UserManager<User> userManager) : Controller
    {
        public async Task<IActionResult> Index()
        {
            // 2. Usas os parâmetros do construtor diretamente no código, sem precisar de variáveis private readonly
            var user = await userManager.GetUserAsync(User);
            var sessions = context.Sessions.ToList();
            
            if (user != null)
            {
                var nome = user.Name; 
            }
            
            IndexViewModel model = new IndexViewModel
            {
                User = user,
                Session = sessions,
                SessionCount = sessions.Count
            };
            
            return View(model);
        }
    }
}