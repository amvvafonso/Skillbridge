using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Skillbridge.Areas.Client.Models;
using Skillbridge.Models.Client;


namespace Skillbridge.Areas.Client.Controllers
{
    [Area("Client")]
    [Authorize]
    public class ClientController : Controller
    {
        // 1. Declaras a variável
        private readonly UserManager<User> _userManager;

        // 2. Injetas no construtor (o ASP.NET entrega-te o UserManager aqui automaticamente)
        public ClientController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // 3. Usas para buscar o utilizador completo da DB
            var user = await _userManager.GetUserAsync(User);
        
            if (user != null)
            {
                var nome = user.Name; // Agora sim, tens o nome!
            }
            
            IndexViewModel model = new IndexViewModel();
            model.User = user;
            return View(model);
        }
    }
}