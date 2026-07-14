using Microsoft.AspNetCore.SignalR;
using Skillbridge.Data;
using Skillbridge.Hubs;
using Skillbridge.Models;

namespace Skillbridge.Services;

public interface INotificationService
{
    Task NotifyOrganizationInviteAsync(string userId, string organizationId, string organizationName);
    Task NotifyAsync(string userId, string message); 

    public class NotificationService(IHubContext<NotificationHub> hubContext, ApplicationDbContext context, ILogger<NotificationService> logger) : INotificationService
    {
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
    }
}