using Microsoft.AspNetCore.Mvc;
using Skillbridge.Data;
using Skillbridge.Models;
using Skillbridge.ViewModels;

namespace Skillbridge.Controllers;

public class OrganizationController(ApplicationDbContext context) : Controller
{
    // GET
    public IActionResult Profile(int id)
    {
        
        var viewModel = new ProfileViewModel
        {
            
            Organization = context.Organizations.Find(id)

        };
        
        if (viewModel.Organization == null) return NotFound();

        return View(viewModel);
    }
}