using Microsoft.AspNetCore.SignalR;
using Skillbridge.Data;
using Skillbridge.Hubs;
using Skillbridge.Models;

namespace Skillbridge.Services;

public interface INotificationService
{
    Task NotifyOrganizationInviteAsync(string userId, string organizationId, string organizationName);

    public class NotificationService(IHubContext<NotificationHub> hubContext, ApplicationDbContext context) : INotificationService
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

            await hubContext.Clients.User(userId).SendAsync("ReceiveNotification", message);
        }
    }
}