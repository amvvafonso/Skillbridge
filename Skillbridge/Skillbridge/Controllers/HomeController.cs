using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Skillbridge.Hubs;
using Skillbridge.Models;

namespace Skillbridge.Controllers;



public class HomeController(IHubContext<NotificationHub> notificationHub) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> TestNotification()
    {
        await notificationHub.Clients.All.SendAsync("ReceiveNotification", "Notificação de teste! 🔔");
        return Ok();
    }
    
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode)
    {
        //Aplica o códito HTTP correto à resposta
        if (statusCode.HasValue) Response.StatusCode = statusCode.Value;

        //Seleciona a view consoante o código de erro
        var viewName = statusCode switch
        {
            403 => "Forbidden",
            _ => "Error"
        };

        return View(viewName, new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}