using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Models.Client;

namespace Skillbridge.Models.Project;

public class SessionAccess
{
    /// <summary>
    /// PK of Access
    /// </summary>
    [Key]
    public string SessionAccessId { get; set; }
    
    
    /// <summary>
    /// Foreign key to link access to session
    /// </summary>
    public string SessionId { get; set; }
    [ForeignKey(nameof(SessionId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public virtual Session Session { get; set; }


    /// <summary>
    /// Role of acess, if is Apprentice, Mentor, it defines if the user can edit the file
    /// </summary>
    public Role Role { get; set; } = Role.Unknown;


}