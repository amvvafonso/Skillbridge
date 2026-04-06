using System.ComponentModel.DataAnnotations;

namespace Skillbridge.Models.Project;

public class ProjectFile
{
    
    /// <summary>
    /// Identifier of the file
    /// </summary>
    [Key]
    private int FileId { get; set; }
    
    /// <summary>
    /// Filename
    /// </summary>
    [Required(ErrorMessage = "Project name is required")]
    [MaxLength(200)]
    public string? Filename { get; set; }
    
    /// <summary>
    /// Path of file
    /// </summary>
    public string? Path { get; set; }
}