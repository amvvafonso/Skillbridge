using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Skillbridge.Data;
using Skillbridge.Models.Project;
using Skillbridge.Models.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Skillbridge.Hubs;
using Skillbridge.Models;
using Skillbridge.Services;
using File = Skillbridge.Models.Project.File;

namespace Skillbridge.Areas.Client.Pages
{
    [Authorize]
    public class CodeEditorModel(IS3Api is3Api, ApplicationDbContext context, UserManager<User> userManager, IHubContext<NotificationHub> notificationHub, ISessionService sessionService) : PageModel
    {
        
        // File 
        public string? FileContent { get; set; }
        public File? CurrentFile {get;set; }
        public Session? CurrentSession { get; set; }
        public User? CurrentUser { get; set; } 
        public Boolean CanEdit { get; set; }
        
        // Chat
        public List<ChatMessage>? ChatMessages { get; set; }
        
        // Adicionar users a sessao
        public List<User> AvailableUsers { get; set; } = new();
        public string SelectedUserEmail { get; set; } = string.Empty;
        public string InviteMessage { get; set; } = string.Empty;
        public bool InviteError { get; set; }

        
        
        //Classe que representa os dados recebidos do editor no POST
        public class SaveRequest
        {
            public string? Content { get; set; }
            //public string SessionId { get; set; }
        }
        
        public async Task<IActionResult> OnGetAsync(string sessionId)
        {
            
            //Vai buscar a sessão à BD, incluindo o ficheiro associado
            CurrentSession = await context.Sessions
                .Include(s => s.file)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (CurrentSession == null) return Forbid();

            
            if (!CurrentSession.Active) return Forbid();
   
            
            //Se o ficheiro nao exisitr, devolve a pagina 404
            if (CurrentSession?.file == null)
            {
                return NotFound();
            }

            CurrentFile = CurrentSession.file;
            
            //Vai buscar o utilizador autenticado
            CurrentUser = await userManager.GetUserAsync(User);
            
            
            //Vai buscar os acessos do utilizador a esta sessão
            var sessionAcess = await context.SessionAccesses
                .FirstOrDefaultAsync(sa => sa.SessionId == CurrentSession.Id && sa.UserId == CurrentUser.Id);
            
            //Sem acesso a esta sessão, devolve a página 403 (Forbiden)
            if (sessionAcess == null)
            {
                return Forbid();
            }


            string? bucket = context.Files
                .Join(context.Project,
                    f => f.ProjectId,
                    p => p.ProjectId,
                    (f, p) => new { f, p })
                .Where(x => x.f.FileId == CurrentFile.FileId)
                .Select(x => x.p.ProjectDirectory)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(bucket))
            {
                return NotFound();
            }
            
            //So consegue editar se tiver o role Mentor na sessão
            CanEdit = sessionAcess.Role == Role.Mentor;
            
            try
            {
                //Vai buscar o conteudo atualizado ao S3
                FileContent = await is3Api.ObterFicheiroAsync(bucket, CurrentFile.Path) ?? string.Empty;
            }
            catch (Amazon.Runtime.AmazonServiceException)
            {
                //Ficheiro ainda nao existe no S3
                FileContent = string.Empty;
            }
            
            //Carrega as mensagens de chat da sessão
            ChatMessages = await context.ChatMessages
                .Where(m=>m.SessionId == CurrentSession.Id)
                .Include(m=>m.User)
                .OrderBy(m=>m.SentAt)
                .ToListAsync();
            
            //Carrega utilizadores disponíveis para convidar (membros da organização que não estão na sessão)
            await LoadAvailableUsersAsync();
            
            return Page();
        }
        
        private async Task LoadAvailableUsersAsync()
        {
            try
            {
                //Vai buscar a OrganizationId através do File -> Project
                var project = await context.Project
                    .FirstOrDefaultAsync(p => p.ProjectId == CurrentFile.ProjectId);
                
                if (project == null) return;
                
                //Vai buscar os membros da organização
                var orgMembers = await context.OrganizationMembers
                    .Where(om => om.Organization == project.OrganizationId)
                    .ToListAsync();
                
                var orgUserIds = orgMembers.Select(om => om.User).ToList();
                
                //Vai buscar os utilizadores que já estão na sessão
                var sessionAccesses = await context.SessionAccesses
                    .Where(sa => sa.SessionId == CurrentSession.Id)
                    .Select(sa => sa.UserId)
                    .ToListAsync();
                
                //Filtra: membros da organização que ainda não estão na sessão
                AvailableUsers = await context.Users
                    .Where(u => orgUserIds.Contains(u.Id) && !sessionAccesses.Contains(u.Id))
                    .ToListAsync();
            }
            catch
            {
                AvailableUsers = new List<User>();
            }
        }

        // Done
        public async Task<IActionResult> OnPostEndSessionAsync([FromForm] string sessionId)
        {
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return LocalRedirect("/Account/Login");
            
            var result = await sessionService.EndSessionAsync(sessionId, userId);

            switch (result.ErrorType)
            {
                case  ErrorType.Denied: return Forbid();
                case  ErrorType.NotFound: return NotFound();
            }

            if (!result.Success) return Page();
            
            // Redireciona para a area de client
            return RedirectToPage("/index", new { area = "Client" });
        }

        // Done
        public async Task<IActionResult> OnPostAddUserToSessionAsync([FromForm] string sessionId, [FromForm] string userEmail)
        {
  
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return LocalRedirect("/Account/Login");
                
            var result = await sessionService.AllowEntrance(sessionId, userEmail, userId, Role.Apprentice);

            switch (result.ErrorType)
            {
                case ErrorType.Denied: return Forbid();
                case ErrorType.NotFound: return NotFound();
            }
            
            if (result.Success) return RedirectToPage("/CodeEditor", new { area = "Client", sessionId });
                
            InviteError = !result.Success;
            InviteMessage = result.Message;
            await LoadAvailableUsersAsync();
            return RedirectToPage("/CodeEditor", new { area = "Client", sessionId = sessionId });
        }

        public async Task<IActionResult> OnPostSaveAsync([FromBody] SaveRequest request)
        {
            //Vai buscar o user autenticado
            var user = await userManager.GetUserAsync(User);
            
            //Vai buscar o sessionId ao URL, assim não é manipulado pelo body o que protege contra CSRF
            var sessionId = Request.Query["sessionId"].ToString();
            
            
            //Vai buscar a sessao a BD incluindo o ficheiro associado
            var session = await context.Sessions
                .Include(s => s.file)
                .FirstOrDefaultAsync(s => s.Id == sessionId);
            
            //Se a sessão não existir ou não tiver ficheiro associado, devolve 404
            if (session?.file == null)
            {
                return NotFound();
            }
            

            //Vai buscar o acesso do utilizador a esta sessao
            var access = await context.SessionAccesses
                .FirstOrDefaultAsync(sa => sa.SessionId == session.Id && sa.UserId == user.Id);

            //Debug
            Console.WriteLine($"SessionId: {sessionId}, UserId: {user.Id}, Role: {access?.Role}, Access null: {access == null}");
            
            //Só mentores podem guardar - viewers recebem 403 Forbidden
            if (access?.Role != Role.Mentor) return StatusCode(403);
            
            //Não permite guardar se a sessão estiver inativa ou bloqueada
            if (!session.Active || session.Locked) return StatusCode(403);
            
            //Guarda o conteudo no S3, usando o Path do ficheiro da própria sessão
            await is3Api.EditarFicheiroAsync("skillbridge",session.file.Path, request.Content);
          
            //Devolve 200 OK para o JS saber que correu bem
            return new OkResult();
        }
    }
}