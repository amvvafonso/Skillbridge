using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Skillbridge.Data;
using Skillbridge.Models.Project;
using Skillbridge.Models.Client;
using Microsoft.AspNetCore.Identity;
using File = Skillbridge.Models.Project.File;

namespace Skillbridge.Areas.Client.Pages
{
    [Authorize]
    public class CodeEditorModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        
        //Injeçao das dependencias do construtor
        public CodeEditorModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public File CurrentFile {get;set; }
        public Session CurrentSession { get; set; }
        public User CurrentUser { get; set; } 
        public Boolean CanEdit { get; set; }
        
        
        public async Task<IActionResult> OnGetAsync(int fileId)
        {
            //Vai buscar o ficheiro a BD pelo Id recebido na query string
            CurrentFile = await _context.Files.FindAsync(fileId);
            
            //Se o ficheiro nao exisitr, devolve a pagina 404
            if (CurrentFile == null)
            {
                return NotFound();
            }
            
            //So vai buscar o utilizador apos confirmar que o ficheiro existe, evitando assim consultas desnecessarias a BD
            CurrentUser = await _userManager.GetUserAsync(User);

            //TO DO Implementar a logica de permissoes por role
            CanEdit = true;
            
            return Page();
        }
    }
}