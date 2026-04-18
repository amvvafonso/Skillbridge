using Microsoft.AspNetCore.Mvc;

namespace Skillbridge.Controllers;

public class InfoController : Controller
{
    // GET
    public IActionResult About()
    {
        return View();
    }

    public IActionResult HowItWorks()
    {
        return View();
    }
}