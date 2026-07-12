using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Data;
using Skillbridge.Hubs;
using Skillbridge.Models;
using Skillbridge.Models.Client;
using Skillbridge.Models.Project;
using Skillbridge.Utilities;
using File = Skillbridge.Models.Project.File;

namespace Skillbridge.Services;

public interface ISessionService
{
    Task<Result> CreateSessionAsync(string bucket, string key, string title, string description, bool isPublic, string userId);
    Task<Result> AllowEntrance(string sessionId, string userEmail, string userId, Role role);
    
    Task<Result> EndSessionAsync(string sessionId, string userId);
    
    public class SessionService(ApplicationDbContext context, IOrganizationService organizationService, INotificationService notificationHub) : ISessionService
    {
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
                    ProjectId = project.ProjectId,
                };
                
                context.Files.Add(file);
                await context.SaveChangesAsync();
                
            }

            if (await FileAlreadyUsedAsync(file))
            {
                return Result.Fail("Já existe uma sessão ativa deste ficheiro!", ErrorType.Misc);
            }
            
            string newSesionId = Guid.NewGuid().ToString();
            
            var newSession = new Session
            {
                Id = newSesionId,
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
                SessionId = newSession.Id,
            };
            
            context.SessionAccesses.Add(initialAccess);
            await context.SaveChangesAsync();

            return Result.Ok(message: "Sessão criada com sucesso!", additional: newSesionId);
        }



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

            if (await organizationService.MemberBelongsToOrganization(project.OrganizationId, userId) != null) return Result.Fail("O utilizador não pertence à organização",  ErrorType.Misc);
            
            if (await AlreadyHasAcessAsync(sessionId, userId)) return Result.Fail("O utilizador já tem acesso!",  ErrorType.Misc);



            var newAccess = new SessionAccess
            {
                SessionAccessId = Guid.NewGuid().ToString(),
                UserId = userInvited.Id,
                Role = role,
                SessionId = session.Id,
            };
            
            context.SessionAccesses.Add(newAccess);
            await context.SaveChangesAsync();

            await notificationHub.NotifyAsync(userEmail, $"Foste adicionado à sessão {session.Title}");
            
            return Result.Ok(message: $"{userInvited.Name} foi adicionado com sucesso!");
        }

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
            return await context.Sessions.AnyAsync(s => s.fileId == file.FileId); 
        }

        private async Task<bool> SessionExistsAsync(string sessionId)
        {
            return await context.Sessions.AnyAsync(s => s.Id == sessionId);
        }

        private async Task<bool> AlreadyHasAcessAsync(string sessionId, string userId)
        {
            return await context.SessionAccesses .AnyAsync(sa => sa.SessionId == sessionId && sa.UserId == userId);
        }

        private async Task<Role> GetRoleAsync(string sessionId, string userId)
        {
            var role = await context.SessionAccesses.FirstOrDefaultAsync(p => p.UserId == userId && p.SessionId == sessionId);
            return role?.Role ?? Role.Unknown;
        }
    }
}