using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Skillbridge.Models;

namespace Skillbridge.Controllers;

/// <inheritdoc />
public class HomeController(ILogger<HomeController> logger) : Controller
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

    /// <summary>
    /// Lida com os erros da aplicação
    /// </summary>
    /// <param name="statusCode"></param>
    /// <returns></returns>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode)
    {
        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerFeature>();
        if (exceptionFeature?.Error != null)
        {
            logger.LogError(exceptionFeature.Error,
                "Erro não tratado em {Path} (StatusCode: {StatusCode})",
                exceptionFeature.Path, statusCode);
        }
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