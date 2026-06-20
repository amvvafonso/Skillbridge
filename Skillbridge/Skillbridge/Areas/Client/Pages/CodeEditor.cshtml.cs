using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Skillbridge.Data;
using Skillbridge.Models.Project;
using Skillbridge.Models.Client;
using Microsoft.AspNetCore.Identity;
using Skillbridge.Models.Utils;
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

        //Classe que representa os dados recebidos do editor no POST
        public class SaveRequest
        {
            public int FileId { get; set; }
            public string Content { get; set; }
        }
        
        public async Task<IActionResult> OnGetAsync(int fileId)
        {
            //Vai buscar o ficheiro a BD pelo Id recebido na query string
            CurrentFile = await _context.Files.FindAsync(fileId);
            
            //Se o ficheiro nao exisitr, devolve a pagina 404
            if (CurrentFile == null)
            {
                return NotFound();
            }

            try
            {
                //Vai buscar o conteudo atualizado ao S3
                var s3 = new S3Api();
                CurrentFile.Content = await s3.ObterFicheiroAsync("skillbridge", CurrentFile.Path);
            }
            catch (Amazon.Runtime.AmazonServiceException)
            {
                //Ficheiro ainda nao existe no S3
                CurrentFile.Content = string.Empty;
            }
            
            //So vai buscar o utilizador apos confirmar que o ficheiro existe, evitando assim consultas desnecessarias a BD
            CurrentUser = await _userManager.GetUserAsync(User);

            //TO DO Implementar a logica de permissoes por role
            CanEdit = true;
            
            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync([FromBody] SaveRequest request)
        {
            //Vai buscar o ficheiro á BD pelo Id 
            var file = await _context.Files.FindAsync(request.FileId);
            
            //Se o ficheiro não existir, devolve 404
            if (file == null)
            {
                return NotFound();
            }
            
            //Guarda o conteudo no S3
            var s3 = new S3Api();
            await s3.EditarFicheiroAsync("skillbridge",file.Path, request.Content);
          
            //Devolve 200 OK para o JS saber que correu bem
            return new OkResult();
        }
    }
}