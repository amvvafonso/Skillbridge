
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Skillbridge.Data;
using Skillbridge.Models;
using Skillbridge.Utilities;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Skillbridge.Models.Client;

namespace Skillbridge.Pages.Organization;

public class IndexModel(ApplicationDbContext context, UserManager<User> userManager) : PageModel
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
    
    [BindProperty]
    public CreateOrganizationInput NewOrganization { get; set; }

    public class CreateOrganizationInput
    {
        [Required(ErrorMessage="O nome é obrigatório")]
        [MaxLength(100)]
        public string OrganizationName { get; set; }
        
        [Required(ErrorMessage="O endereço é obrigatório")]
        [MaxLength(200)]
        public string OrganizationAddress { get; set; }
        
        [MaxLength(1000)]
        public string? OrganizationDescription { get; set; }
    }

    public async Task<IActionResult> OnPostCreateOrganizationAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Index");

        if (!ModelState.IsValid)
        {
            //Recarrega os dados do Dashboard para a página não arrebentar ao voltar a renderizar
            Search();
            return Page();
        }

        var organization = new Models.Organization
        {
            OrganizationId = Guid.NewGuid().ToString(),
            OrganizationName = NewOrganization.OrganizationName,
            OrganizationAddress = NewOrganization.OrganizationAddress,
            OrganizationDescription = NewOrganization.OrganizationDescription,
            Owner = user.Id
        };

        context.Organizations.Add(organization);
        await context.SaveChangesAsync();

        return RedirectToPage();
    }
        
}

