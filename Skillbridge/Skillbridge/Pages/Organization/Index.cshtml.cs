using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Skillbridge.Data;
using Skillbridge.Models;
using Skillbridge.Utilities;

namespace Skillbridge.Pages.Organization;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public ICollection<Skillbridge.Models.Organization> Organizations { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }
    public int Count { get; set; }
    
    public void OnGet()
    {
        var todas = context.Organizations.ToList(); // traz tudo para memória
        Count = todas.Count;
        if (string.IsNullOrWhiteSpace(Q))
        {
            Organizations = todas;
            return;
        }
        
        Organizations = todas
            .Where(o =>
                Levenshtein.Contem(o.OrganizationName, Q) ||
                Levenshtein.Contem(o.OrganizationAddress, Q) ||
                (o.OrganizationDescription != null && Levenshtein.Contem(o.OrganizationDescription, Q))
            )
            .ToList();
    }

// Verifica se alguma "palavra" do texto está próxima da pesquisa, ou se a pesquisa aparece literalmente
    
    
}