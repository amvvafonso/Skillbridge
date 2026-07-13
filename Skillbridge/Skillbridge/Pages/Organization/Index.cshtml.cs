using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Skillbridge.Data;
using Skillbridge.Models;
using Skillbridge.Utilities;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Skillbridge.Models.Client;
using Skillbridge.Services;

namespace Skillbridge.Pages.Organization;

public class IndexModel(ApplicationDbContext context, IS3Api s3Api, IOrganizationService organizationService) : PageModel
{
    
    public ICollection<Skillbridge.Models.Organization> Organizations { get; set; } = [];
    
    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    public int Count { get; set; }

    public void OnGet()
    {
        Search();
    }

    public PartialViewResult OnGetSearch()
    {
        Search();
        return Partial("_OrganizationResults", this);
    }

    private void Search()
    {
        var todas = context.Organizations.ToList(); // traz tudo para memória
        Count = todas.Count;

        if (string.IsNullOrWhiteSpace(Q))
        {
            Organizations = todas;
            Count = todas.Count;
            return;
        }

        Organizations = todas
            .Where(o =>
                Levenshtein.Contem(o.OrganizationName, Q) ||
                Levenshtein.Contem(o.OrganizationAddress, Q) ||
                (o.OrganizationDescription != null && Levenshtein.Contem(o.OrganizationDescription, Q))
            )
            .ToList();

        Count = Organizations.Count;
    }
    

    
    public async Task<IActionResult> OnPostCreateOrganizationAsync([FromForm] string organizationName, [FromForm] string organizationAddress,  [FromForm] string organizationDescription, [FromForm] IFormFile logoInput)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Forbid();
            
        var result = await organizationService.CreateOrganizationAsync(userId, organizationName, organizationAddress, organizationDescription, logoInput);

        switch (result.ErrorType)
        {
            case ErrorType.Denied: return Forbid();
            case ErrorType.NotFound: return NotFound();
        }
        
        
        
        return RedirectToPage();
    }
    
    public async Task<IActionResult> OnGetAvatarAsync(string key)
    {
        var image = await s3Api.GetBinaryAsync("logos", key);

        if (image == null)
            return NotFound();

        return File(image.Value.Data, image.Value.ContentType);
    }
}

