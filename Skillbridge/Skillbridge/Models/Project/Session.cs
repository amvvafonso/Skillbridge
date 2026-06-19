using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Skillbridge.Models.Project;

public class Session
{   
    /// <summary>
    /// ID of session
    /// </summary>
    [Key]
    public string? Id { get; set; }
    
    /// <summary>
    /// Determines if session is active and people can join
    /// </summary>
    public bool Active { get; set; }
    
    /// <summary>
    /// Date of creation
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Datetime of when it starts
    /// </summary>
    public DateTime StartsAt { get; set; }
    
    /// <summary>
    /// Title of session
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string? Title { get; set; }
    
    /// <summary>
    /// Determines if session is public or closed
    /// </summary>
    [Required]
    public bool isPublic  { get; set; }
    
    /// <summary>
    /// Determines if the session is locked, no operation is permited and chat is closed
    /// </summary>
    public bool Locked { get; set; }
    
    /// <summary>
    /// Small description of session, max 200
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Description { get; set; }
     
    /// <summary>
    /// Foreign key, file of session, content to be displayed
    /// </summary>
    public int fileId { get; set; }
    [ForeignKey(nameof(fileId))]
    public virtual File file { get; set; }
    
    
}