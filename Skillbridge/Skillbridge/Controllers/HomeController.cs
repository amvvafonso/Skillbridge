using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Skillbridge.Models;

namespace Skillbridge.Controllers;



public class HomeController() : Controller
{
    public async Task<IActionResult> Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> TestNotification()
    {
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