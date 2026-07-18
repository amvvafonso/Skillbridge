using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Data;
using Skillbridge.Hubs;
using Skillbridge.Models;
using Skillbridge.Models.Client;
using Skillbridge.Utilities;

namespace Skillbridge.Services;

/// <summary>
/// Serviço responsável pela gestão de notificações, incluindo convites para
/// organizações, envio em tempo real via SignalR e persistência na base de dados
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Regista na base de dados e envia em tempo real um convite de organização
    /// a um utilizador se o envio em tempo real falhar, a notificação
    /// permanece guardada e visível quando o user consultar as notificações
    /// </summary>
    /// <param name="userId">Identificador do utilizador convidado</param>
    /// <param name="organizationId">Identificador da organização que enviou o convite</param>
    /// <param name="organizationName">Nome da organização, usado na mensagem da notificação</param>
    Task NotifyOrganizationInviteAsync(string userId, string organizationId, string organizationName);
   
    /// <summary>
    /// Envia uma notificação genérica em tempo real a um user via SignalR,
    /// sem a persistir na base de dados
    /// </summary>
    /// <param name="userId">Identificador do user a notificar</param>
    /// <param name="message">Conteúdo da mensagem a enviar</param>
    Task NotifyAsync(string userId, string message);
    
    /// <summary>
    /// Aceita um convite de organização, adicionando o utilizador como membro
    /// com o papel de Apprentice, caso ainda não seja membro. Marca a notificação como oculta.
    /// </summary>
    /// <param name="notificationId">Identificador da notificação de convite</param>
    /// <param name="userId">Identificador do user que aceita o convite</param>
    /// <returns>Um <see cref="Result"/> indicando sucesso ou falha da operação</returns>
    Task<Result> AcceptOrganizationInviteAsync(string notificationId, string userId);
    
    /// <summary>
    /// Rejeita um convite de organização, marcando a notificação como oculta
    /// sem adicionar o user como membro
    /// </summary>
    /// <param name="notificationId">Identificador da notificação de convite</param>
    /// <param name="userId">Identificador do user que rejeita o convite</param>
    /// <returns>Um <see cref="Result"/> indicando sucesso ou falha da operação</returns>
    Task<Result> DeclineOrganizationInviteAsync(string notificationId, string userId);
    
    /// <summary>
    /// Obtém todas as notificações não ocultas pertencentes a um user
    /// </summary>
    /// <param name="userId">Identificador do user</param>
    /// <returns>Lista de <see cref="Notification"/> visíveis</returns>
    Task<List<Notification>> GetNotificationAsync(string userId);

    /// <inheritdoc />
    public class NotificationService(IHubContext<NotificationHub> hubContext, ApplicationDbContext context, ILogger<NotificationService> logger) : INotificationService
    {
        /// <inheritdoc />
        public async Task NotifyOrganizationInviteAsync(string userId, string organizationId, string organizationName)
        {
            var message = $"Foste convidado para a organização {organizationName}";

            var np = new NotificationParam
            {
                Param = organizationId,
                Other = new Dictionary<string, string> { { "message", message } }
            };

            context.Notifications.Add(new Notification(np, userId, NotificationType.OrganizationInvite));
            await context.SaveChangesAsync();

            try
            {
                await hubContext.Clients.User(userId).SendAsync("ReceiveNotification", message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Notificação de convite guardada na BD, mas falhou o envio em tempo real ao utilizador {UserId}", userId);
            }
        }
        /// <inheritdoc />
        public async Task NotifyAsync(string userId, string message)
        {
            try
            {
                await hubContext.Clients.User(userId).SendAsync("ReceiveNotification", message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao enviar notificação em tempo real ao utilizador {UserId}", userId);
            }
        }

        /// <inheritdoc />
        public async Task<Result> AcceptOrganizationInviteAsync(string notificationId, string userId)
        {
            if (string.IsNullOrEmpty(notificationId) || string.IsNullOrEmpty(userId)) return Result.Fail("Falta componentes crucais", ErrorType.MissingComponent);

            var notif = await context.Notifications.FindAsync(notificationId);
            if (notif == null)
                return Result.Fail("Não existe nenhuma notificação", ErrorType.NotFound);
            
            string param = notif.Param;

            if (notif.Type != NotificationType.OrganizationInvite)
                return Result.Fail("Tipo de notificação não é adequada");

            var alreadyMember = await context.OrganizationMembers
                .AnyAsync(m => m.Organization == param && m.User == userId);
    
            if (!alreadyMember)
            {
                await context.OrganizationMembers.AddAsync(new OrganizationMember(Guid.NewGuid().ToString(),param, userId, Role.Apprentice));
            }
                    
            notif.Hidden = true;
                    
            await context.SaveChangesAsync(); 
            
            return Result.Ok("Convite aceitado!");
        }

        public async Task<Result> DeclineOrganizationInviteAsync(string notificationId, string userId)
        {
            if (string.IsNullOrEmpty(notificationId) || string.IsNullOrEmpty(userId)) return Result.Fail("Falta componentes crucais", ErrorType.MissingComponent);

            var notif = await context.Notifications.FindAsync(notificationId);
            
            if (notif == null)
                return Result.Fail("Não existe nenhuma notificação", ErrorType.NotFound);
            
            notif.Hidden = true;
            context.Notifications.Update(notif);
            await context.SaveChangesAsync();
            
            return Result.Ok("Convite negado com sucesso!");
        }

        /// <inheritdoc />
        public async Task<List<Notification>> GetNotificationAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return [];

            var notifications = await context.Notifications
                .Where(p => p.UserId == userId && !p.Hidden)
                .ToListAsync();

            return notifications;
        }
    }
}