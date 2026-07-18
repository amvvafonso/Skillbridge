using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Skillbridge.Data;
using Skillbridge.Hubs;
using Skillbridge.Models;
using Skillbridge.Models.Client;
using Skillbridge.Models.Project;
using Skillbridge.Utilities;
using File = Skillbridge.Models.Project.File;

namespace Skillbridge.Services;

/// <summary>
/// Serviço responsável pela gestão de sessões de colaboração em tempo real
/// sobre ficheiros de projeto, incluindo controlo de acessos e notificações
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Obtém todas as sessões a que o utilizador tem acesso, incluindo dados do ficheiro associado,
    /// ordenadas pela data de criação mais recente
    /// </summary>
    /// <param name="userid">Identificador do utilizador</param>
    /// <returns>Lista de <see cref="Session"/> acessíveis pelo utilizador</returns>
    Task<List<Session>> GetAllSessionsAsync(string userid);
    
    /// <summary>
    /// Obtém uma sessão específica pelo seu identificador
    /// </summary>
    /// <param name="sessionId">Identificador da sessão</param>
    /// <returns>A <see cref="Session"/> correspondente, ou <c>null</c> se não existir</returns>
    Task<Session?> GetSessionAsync(string sessionId);
    
    /// <summary>
    /// Cria uma sessão de colaboração associada a um ficheiro, se o ficheiro ainda
    /// não existir na base de dados, é criado automaticamente.
    /// O utilizador criador recebe automaticamente o papel de Mentor na sessão
    /// </summary>
    /// <param name="bucket">Nome do diretório do projeto onde o ficheiro se encontra</param>
    /// <param name="key">Caminho do ficheiro associado à sessão</param>
    /// <param name="title">Título da sessão</param>
    /// <param name="description">Descrição da sessão</param>
    /// <param name="isPublic">Indica se a sessão é pública</param>
    /// <param name="userId">Identificador do utilizador que cria a sessão</param>
    /// <returns>Um <see cref="Result"/> com o identificador da nova sessão em caso de sucesso</returns>

    Task<Result> CreateSessionAsync(string bucket, string key, string title, string description, bool isPublic, string userId);
    
    /// <summary>
    /// Concede acesso a um utilizador convidado por email a uma sessão ativa, apenas utilizadores com papel Mentor na sessão podem convidar novos membros
    /// </summary>
    /// <param name="sessionId">Identificador da sessão</param>
    /// <param name="userEmail">Email do utilizador a convidar</param>
    /// <param name="userId">Identificador do utilizador que envia o convite (deve ser Mentor da sessão)</param>
    /// <param name="role">Papel a atribuir ao utilizador convidado dentro da sessão</param>
    /// <returns>Um <see cref="Result"/> indicando sucesso ou falha da operação</returns>
    Task<Result> AllowEntrance(string sessionId, string userEmail, string userId, Role role);
    
    /// <summary>
    /// Termina uma sessão de colaboração ativa, apenas um utilizador com papel Mentor na sessão pode terminá-la
    /// </summary>
    /// <param name="sessionId">Identificador da sessão a terminar</param>
    /// <param name="userId">Identificador do utilizador que solicita o encerramento</param>
    /// <returns>Um <see cref="Result"/> indicando sucesso ou falha da operação</returns>
    Task<Result> EndSessionAsync(string sessionId, string userId);


    /// <inheritdoc />
    public class SessionService(ApplicationDbContext context, IOrganizationService organizationService, INotificationService notificationHub) : ISessionService
    {
        /// <inheritdoc />
        public async Task<List<Session>> GetAllSessionsAsync(string userid)
        {
            var userSessions = await context.SessionAccesses
                .Where(sa => sa.UserId == userid)
                .Include(sa => sa.Session)
                .ThenInclude(s => s.file)
                .Select(sa => sa.Session)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
            
            return userSessions;
        }

        /// <inheritdoc />
        public async Task<Session?> GetSessionAsync(string sessionId)
        {
            var session = await context.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            
            return session;
        }


        /// <inheritdoc />
        public async Task<Result> CreateSessionAsync(string bucket, string key, string title, string description, bool isPublic, string userId)
        {
            var file = await context.Files.FirstOrDefaultAsync(f => f.Path == key);
            if (file == null)
            {
                var project = await context.Project.FirstOrDefaultAsync(p => p.ProjectDirectory == bucket);
                if (project == null) return Result.Fail("O diretório não existe!", ErrorType.NotFound);

                file = new File
                {
                    FileId = Guid.NewGuid().ToString(),
                    Path = key,
                    Locked = false,
                    ProjectId = project.ProjectId
                };
                
                context.Files.Add(file);
                await context.SaveChangesAsync();
                
            }

            if (await FileAlreadyUsedAsync(file))
            {
                return Result.Fail("Já existe uma sessão ativa deste ficheiro!");
            }
            
            string newSessionId = Guid.NewGuid().ToString();
            
            var newSession = new Session
            {
                Id = newSessionId,
                Title = title.Trim(),
                Description = description.Trim(),
                IsPublic = isPublic,
                fileId = file.FileId,
                Active = true,
                CreatedAt = DateTime.UtcNow,
                Locked = false
            };
            
            context.Sessions.Add(newSession);
            await context.SaveChangesAsync();
            
            // Required, if not no one has access to session
            var initialAccess = new SessionAccess
            {
                SessionAccessId = Guid.NewGuid().ToString(),
                UserId = userId,
                Role = Role.Mentor, // If not mentor, no one can alter the properties of the session
                SessionId = newSession.Id
            };
            
            context.SessionAccesses.Add(initialAccess);
            await context.SaveChangesAsync();

            return Result.Ok(message: "Sessão criada com sucesso!", additional: newSessionId);
        }


        /// <inheritdoc />
        public async Task<Result> AllowEntrance(string sessionId, string userEmail, string userId, Role role)
        {
            if (string.IsNullOrEmpty(sessionId)) return Result.Fail("Não foi selecionado uma sessão!", ErrorType.NotFound);

            if (string.IsNullOrEmpty(userEmail)) return Result.Fail("É obrigatório fornecer uma email!", ErrorType.MissingComponent);

            var hasAccess = await context.SessionAccesses
                .AnyAsync(sa => sa.SessionId == sessionId && sa.UserId == userId && sa.Role == Role.Mentor);
            if (!hasAccess) return Result.Fail("Apenas mentores podem adicionar membros à sessão!",  ErrorType.Denied);
            
            var session = await context.Sessions
                .Include(s => s.file)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null) return Result.Fail("Não existe nenhuma sessão com esse identificador!", ErrorType.NotFound);
            
            if (!session.Active) return Result.Fail("A sessão encontra-se desativa", ErrorType.NotFound);
            
            var userInvited =  await context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (userInvited == null) return Result.Fail("Não existe nenhum utilizador com email fornecido!", ErrorType.MissingComponent);
            
            var project = await context.Project
                .FirstOrDefaultAsync(p => p.ProjectId == session.file.ProjectId);

            if (project == null) return Result.Fail("O projeto não existe!", ErrorType.NotFound);

            if (await organizationService.MemberBelongsToOrganization(project.OrganizationId, userId) == null) 
                return Result.Fail("O utilizador não pertence à organização");
            
            if (await AlreadyHasAcessAsync(sessionId, userInvited.Id)) return Result.Fail("O utilizador já tem acesso!");


            if (session.Id != null)
            {
                var newAccess = new SessionAccess
                {
                    SessionAccessId = Guid.NewGuid().ToString(),
                    UserId = userInvited.Id,
                    Role = role,
                    SessionId = session.Id
                };
            
                context.SessionAccesses.Add(newAccess);
            }

            await context.SaveChangesAsync();

            await notificationHub.NotifyAsync(userInvited.Id, $"Foste adicionado à sessão {session.Title}");
            
            return Result.Ok(message: $"{userInvited.Name} foi adicionado com sucesso!");
        }

        /// <inheritdoc />
        public async Task<Result> EndSessionAsync(string sessionId, string userId)
        {
            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(userId))
                return Result.Fail("É obrigatório fornecer uma sessão e estar autenticado!",
                    ErrorType.MissingComponent);
            if (await GetRoleAsync(sessionId, userId) != Role.Mentor) return Result.Fail("Não tem permissão para terminar a sessão!",  ErrorType.Denied);

            var session = await context.Sessions
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null) return Result.Fail("Não existe a sessão!", ErrorType.NotFound);
            
            session.Active = false;
            await context.SaveChangesAsync();

            return Result.Ok("Sessão terminada com sucesso!");
        }


        private async Task<bool> FileAlreadyUsedAsync(File file)
        {
            return await context.Sessions.AnyAsync(s => s.fileId == file.FileId && s.Active); 
        }

        private async Task<bool> AlreadyHasAcessAsync(string sessionId, string userId)
        {
            return await context.SessionAccesses.AnyAsync(sa => sa.SessionId == sessionId && sa.UserId == userId);
        }

        private async Task<Role> GetRoleAsync(string sessionId, string userId)
        {
            var role = await context.SessionAccesses.FirstOrDefaultAsync(p => p.UserId == userId && p.SessionId == sessionId);
            return role?.Role ?? Role.Unknown;
        }
    }
}