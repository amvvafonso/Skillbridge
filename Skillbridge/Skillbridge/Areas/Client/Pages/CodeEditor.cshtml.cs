using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Skillbridge.Data;
using Skillbridge.Models.Project;
using Skillbridge.Models.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualBasic;
using Skillbridge.Hubs;
using Skillbridge.Models.Utils;
using File = Skillbridge.Models.Project.File;

namespace Skillbridge.Areas.Client.Pages
{
    [Authorize]
    public class CodeEditorModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IHubContext<NotificationHub> _notificationHub;   
        private readonly S3Api s3Api;
        public CodeEditorModel(ApplicationDbContext context, UserManager<User> userManager, IHubContext<NotificationHub> notificationHub, S3Api s3Api)
        {
            _context = context;
            _userManager = userManager;
            this._notificationHub = notificationHub;
            this.s3Api = s3Api;
        }
        
        // File 
        public string FileContent { get; set; }
        public File CurrentFile {get;set; }
        public Session CurrentSession { get; set; }
        public User CurrentUser { get; set; } 
        public Boolean CanEdit { get; set; }
        
        // Chat
        public List<ChatMessage> ChatMessages { get; set; }
        
        // Adicionar users a sessao
        public List<User> AvailableUsers { get; set; } = new();
        public string SelectedUserEmail { get; set; } = string.Empty;
        public string InviteMessage { get; set; } = string.Empty;
        public bool InviteError { get; set; }

        
        
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

            if (!CurrentSession.Active) return Forbid();
   
            
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
                FileContent = await s3Api.ObterFicheiroAsync("skillbridge", CurrentFile.Path) ?? string.Empty;
            }
            catch (Amazon.Runtime.AmazonServiceException)
            {
                //Ficheiro ainda nao existe no S3
                FileContent = string.Empty;
            }
            
            //Carrega as mensagens de chat da sessão
            ChatMessages = await _context.ChatMessages
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
                var project = await _context.Project
                    .FirstOrDefaultAsync(p => p.ProjectId == CurrentFile.ProjectId);
                
                if (project == null) return;
                
                //Vai buscar os membros da organização
                var orgMembers = await _context.OrganizationMembers
                    .Where(om => om.Organization == project.OrganizationId)
                    .ToListAsync();
                
                var orgUserIds = orgMembers.Select(om => om.User).ToList();
                
                //Vai buscar os utilizadores que já estão na sessão
                var sessionAccesses = await _context.SessionAccesses
                    .Where(sa => sa.SessionId == CurrentSession.Id)
                    .Select(sa => sa.UserId)
                    .ToListAsync();
                
                //Filtra: membros da organização que ainda não estão na sessão
                AvailableUsers = await _context.Users
                    .Where(u => orgUserIds.Contains(u.Id) && !sessionAccesses.Contains(u.Id))
                    .ToListAsync();
            }
            catch
            {
                AvailableUsers = new List<User>();
            }
        }

        public async Task<IActionResult> OnPostEndSessionAsync([FromForm] string sessionId)
        {
            try
            {
                // Vai buscar user logado
                var user = await _userManager.GetUserAsync(User);

                // Vai buscar a sessão
                var session = await _context.Sessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId);

                if (session == null) return NotFound();

                //Verifica se o utilizador tem acesso à sessão
                var access = await _context.SessionAccesses
                    .FirstOrDefaultAsync(sa => sa.SessionId == session.Id && sa.UserId == user.Id);

                //Só Mentor pode terminar a sessão
                if (access?.Role != Role.Mentor) return Forbid();
                
                // Atualiza o estado da sessao para inativa
                session.Active = false;
                await _context.SaveChangesAsync();
                
                // Redireciona para a area de client
                return RedirectToPage("/index", new { area = "Client" });

            }
            catch (Exception es)
            {
                return RedirectToPage("/CodeEditor", new { area = "Client", sessionId });
            }
        }

        public async Task<IActionResult> OnPostAddUserToSessionAsync([FromForm] string? sessionId, [FromForm] string? userEmail)
        {
            try
            {
                // Verifica que existe uma session id
                if (sessionId == null) return NotFound();
                
                var user = await _userManager.GetUserAsync(User);

                //Vai buscar o email do corpo do pedido
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    InviteError = true;
                    InviteMessage = "Email é obrigatório.";
                    await LoadAvailableUsersAsync();
                    return RedirectToPage("/CodeEditor", new { area = "Client", sessionId = sessionId });

                }
                
                var session = await _context.Sessions
                    .Include(s => s.file)
                    .FirstOrDefaultAsync(s => s.Id == sessionId);
                // Verifica que existe um ficheiro associado a sessao
                if (session?.file == null) return NotFound();

                //Verifica se o utilizador é Mentor na sessão
                var access = await _context.SessionAccesses
                    .FirstOrDefaultAsync(sa => sa.SessionId == session.Id && sa.UserId == user.Id);
                
                if (access?.Role != Role.Mentor) return Forbid();

                //Verifica se a sessão ainda está ativa
                if (!session.Active)
                {
                    InviteError = true;
                    InviteMessage = "A sessão encontra-se inativa.";
                    await LoadAvailableUsersAsync();
                    return RedirectToPage("/CodeEditor", new { area = "Client", sessionId = sessionId });

                }
                
                //Encontra o utilizador a convidar
                var invitee = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == userEmail);

                if (invitee == null)
                {
                    InviteError = true;
                    InviteMessage = "Utilizador não encontrado.";
                    await LoadAvailableUsersAsync();
                    return RedirectToPage("/CodeEditor", new { area = "Client", sessionId = sessionId });

                }
                
                //Verifica se pertence à organização do projeto
                var project = await _context.Project
                    .FirstOrDefaultAsync(p => p.ProjectId == session.file.ProjectId);

                if (project == null) return NotFound();

                var isMember = await _context.OrganizationMembers
                    .AnyAsync(om => om.Organization == project.OrganizationId && om.User == invitee.Id);

                if (!isMember)
                {
                    InviteError = true;
                    InviteMessage = "Utilizador não pertence à organização.";
                    await LoadAvailableUsersAsync();
                    return RedirectToPage("/CodeEditor", new { area = "Client", sessionId  });

                }

                //Verifica se já tem acesso à sessão
                var existingAccess = await _context.SessionAccesses
                    .FirstOrDefaultAsync(sa => sa.SessionId == session.Id && sa.UserId == invitee.Id);

                if (existingAccess != null)
                {
                    InviteError = true;
                    InviteMessage = "Utilizador já está na sessão.";
                    await LoadAvailableUsersAsync();
                    return RedirectToPage("/CodeEditor", new { area = "Client", sessionId = session.Id });

                }

                //Cria o acesso à sessão como Apprentice
                var newAccess = new SessionAccess
                {
                    SessionAccessId = Guid.NewGuid().ToString(),
                    SessionId = session.Id,
                    UserId = invitee.Id,
                    Role = Role.Apprentice
                };

                _context.SessionAccesses.Add(newAccess);
                await _context.SaveChangesAsync();

                InviteMessage = $"{invitee.Name} foi adicionado à sessão.";
                InviteError = false;

                await LoadAvailableUsersAsync();
                
                await _notificationHub.Clients.User(invitee.Id)
                    .SendAsync("ReceiveNotification", $"Foste adicionado à sessão {session.Title}");
                
                return RedirectToPage("/CodeEditor", new { area = "Client", sessionId });

            }
            catch (Exception ex)
            {
                Console.WriteLine((ex.InnerException != null ? ex.InnerException.Message : ex.Message));
                return RedirectToPage("/CodeEditor", new { area = "Client", sessionId });
            }
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
            await s3Api.EditarFicheiroAsync("skillbridge",session.file.Path, request.Content);
          
            //Devolve 200 OK para o JS saber que correu bem
            return new OkResult();
        }
    }
}