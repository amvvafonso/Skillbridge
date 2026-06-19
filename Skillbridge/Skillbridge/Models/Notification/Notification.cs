using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Skillbridge.Models.Client;

namespace Skillbridge.Models;

public class Notification
{
    /// <summary>
    /// Id of notification
    /// </summary>
    [Key]
    public string NotificationId { get; set; }
    
    /// <summary>
    /// Title of notification
    /// </summary>
    public string Title { get; set; }
    
    /// <summary>
    /// Body/Content of notification
    /// </summary>
    public string Body { get; set; }
    
    /// <summary>
    /// Param of invite, most of the times it will be organizationId
    /// </summary>
    public string Param { get; set; }
    
    /// <summary>
    /// Timestamp of notification
    /// </summary>
    public DateTime Date { get; set; }
    
    /// <summary>
    /// Links notification to user
    /// </summary>
    public string UserId { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; }
    
    /// <summary>
    /// Determines the type of notification Enum -> Invite or Information
    /// </summary>
    public NotificationType Type { get; set; }
    
    /// <summary>
    /// Determines if the notification is hidden, user option or timer
    /// </summary>
    public bool Hidden { get; set; }
    
}