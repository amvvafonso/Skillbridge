using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Models.Client;

namespace Skillbridge.Models.Project;

public class ChatMessage
{
    /// <summary>
    /// PK - Id of message
    /// </summary>
    [Key]
    public string ChatMessageId { get; set; }
    
    /// <summary>
    /// Content of message
    /// </summary>
    [Required]
    [Display(Name = "Chat Message")]
    [DataType(DataType.MultilineText)]
    public string ChatMessageText { get; set; }
    
    /// <summary>
    /// User that sent the message
    /// </summary>
    public string UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public User User { get; set; }
    
    /// <summary>
    /// Foreign key, links chat message to session
    /// </summary>
    public string SessionId { get; set; }
    [ForeignKey(nameof(SessionId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public virtual Session Session { get; set; }
}