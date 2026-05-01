using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skillbridge.Models;

namespace Skillbridge.Areas.Client.Controllers
{
    [Area("Client")]
    [Authorize]
    public class ClientController : Controller
    {
        
        public IActionResult Index()
        {
            return View();
        }
    }
}