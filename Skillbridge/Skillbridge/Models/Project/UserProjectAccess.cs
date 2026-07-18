using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Models.Client;

namespace Skillbridge.Models.Project;

public class UserProjectAccess
{
    /// <summary>
    /// Identifier of access record
    /// </summary>
    [Key]
    public int AccessId { get; set; }

    /// <summary>
    /// Role for Project
    /// </summary>
    public Role ProjectRole { get; set; } = Role.Unknown;
    
    // ############################################################
    // Relacionamentos M-N
    // ############################################################
    
    /// <summary>
    /// FK for User (String because IdentityUser)
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;
    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }
    
    /// <summary>
    /// FK for Project
    /// </summary>
    public int ProjectId { get; set; }
    [ForeignKey(nameof(ProjectId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public virtual Project? Project { get; set; }

    
    public UserProjectAccess(Role projectRole, string? userId, int projectId)
    {
        ProjectRole = projectRole;
        UserId = userId;
        ProjectId = projectId;
    }


}