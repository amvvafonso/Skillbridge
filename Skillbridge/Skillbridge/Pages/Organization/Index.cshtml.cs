using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Skillbridge.Data;
using Skillbridge.Models;
using Skillbridge.Utilities;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Skillbridge.Models.Client;
using Skillbridge.Models.Utils;

namespace Skillbridge.Pages.Organization;

public class IndexModel(ApplicationDbContext context, UserManager<User> userManager, S3Api s3Api) : PageModel
{
    private readonly S3Api s3Api = s3Api;
    
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

        public IFormFile? LogoFile { get; set; }
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

        var guid = Guid.NewGuid().ToString();

        string logoPath = "/default_logo.png";
        if (NewOrganization.LogoFile != null && NewOrganization.LogoFile.Length > 0)
        {
            
            using var ms = new System.IO.MemoryStream();
            await NewOrganization.LogoFile.CopyToAsync(ms);
            var bytes = ms.ToArray();
            

            var success = await s3Api.UploadBinaryAsync("logos", $"{guid}.png", bytes, NewOrganization.LogoFile.ContentType);
            if (success) logoPath = $"{guid}.png";
        }

        var organization = new Models.Organization
        {
            OrganizationId = guid,
            OrganizationName = NewOrganization.OrganizationName,
            OrganizationAddress = NewOrganization.OrganizationAddress,
            OrganizationDescription = NewOrganization.OrganizationDescription,
            Owner = user.Id,
            LogoPath = logoPath
        };

        context.Organizations.Add(organization);

        var ogm = new OrganizationMember(Guid.NewGuid().ToString(), guid, user.Id, Role.Owner);
        context.OrganizationMembers.Add(ogm);
        
        await context.SaveChangesAsync();

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

