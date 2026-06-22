using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Skillbridge.Data;
using Skillbridge.Models.Project;
using Skillbridge.Models.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.VisualBasic;
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
            public string Content { get; set; }
            //public string SessionId { get; set; }
        }
        
        public async Task<IActionResult> OnGetAsync(string sessionId)
        {
            //Vai buscar a sessão à BD, incluindo o ficheiro associado
            CurrentSession = await _context.Sessions
                .Include(s => s.file)
                .FirstOrDefaultAsync(s => s.Id == sessionId);
            
            //Se o ficheiro nao exisitr, devolve a pagina 404
            if (CurrentSession?.file == null)
            {
                return NotFound();
            }

            CurrentFile = CurrentSession.file;
            
            //Vai buscar o utilizador autenticado
            CurrentUser = await _userManager.GetUserAsync(User);
            
            
            //Vai buscar os acessos do utilizador a esta sessão
            var sessionAcess = await _context.SessionAccesses
                .FirstOrDefaultAsync(sa => sa.SessionId == CurrentSession.Id && sa.UserId == CurrentUser.Id);
            
            //Sem acesso a esta sessão, devolve a página 403 (Forbiden)
            if (sessionAcess == null)
            {
                return Forbid();
            }
            
            //So consegue editar se tiver o role Mentor na sessão
            CanEdit = sessionAcess.Role == Role.Mentor;
            
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
            
            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync([FromBody] SaveRequest request)
        {
            //Vai buscar o user autenticado
            var user = await _userManager.GetUserAsync(User);
            
            //Vai buscar o sessionId ao URL, assim não é manipulado pelo body o que protege contra CSRF
            var sessionId = Request.Query["sessionId"].ToString();
            
            
            //Vai buscar a sessao a BD incluindo o ficheiro associado
            var session = await _context.Sessions
                .Include(s => s.file)
                .FirstOrDefaultAsync(s => s.Id == sessionId);
            
            //Se a sessão não existir ou não tiver ficheiro associado, devolve 404
            if (session?.file == null)
            {
                return NotFound();
            }
            

            //Vai buscar o acesso do utilizador a esta sessao
            var access = await _context.SessionAccesses
                .FirstOrDefaultAsync(sa => sa.SessionId == session.Id && sa.UserId == user.Id);

            //Debug
            Console.WriteLine($"SessionId: {sessionId}, UserId: {user.Id}, Role: {access?.Role}, Access null: {access == null}");
            
            //Só mentores podem guardar - viewers recebem 403 Forbidden
            if (access?.Role != Role.Mentor) return StatusCode(403);
            
            //Não permite guardar se a sessão estiver inativa ou bloqueada
            if (!session.Active || session.Locked) return StatusCode(403);
            
            //Guarda o conteudo no S3, usando o Path do ficheiro da própria sessão
            var s3 = new S3Api();
            await s3.EditarFicheiroAsync("skillbridge",session.file.Path, request.Content);
          
            //Devolve 200 OK para o JS saber que correu bem
            return new OkResult();
        }
    }
}