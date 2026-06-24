using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Skillbridge.Models.Project;

public class File
{
    /// <summary>
    /// Identifier of the file
    /// </summary>
    [Key]
    public string FileId { get; set; }

    /// <summary>
    /// Manages if file is locked
    /// </summary>
    public bool Locked { get; set; }
    
    /// <summary>
    /// Path of file
    /// </summary>
    [Required]
    public string? Path { get; set; }
    
    
    // ############################################################
    // Relacionamentos 1-N
    // ############################################################
    
    
    /// <summary>
    /// Foreign key of Project
    /// </summary>
    public int ProjectId { get; set; }
    [ForeignKey(nameof(ProjectId))]
    public virtual Project? Project { get; set; }
    
    
    
}
