using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Skillbridge.Models.Project;

public class File
{
    /// <summary>
    /// Identifier of the file
    /// </summary>
    [Key]
    private int FileId { get; set; }

    /// <summary>
    /// Manages if file is locked
    /// </summary>
    public bool Locked { get; set; }

    /// <summary>
    /// Name of the file, duh
    /// </summary>
    [Required(ErrorMessage = "Project name is required")]
    [MaxLength(200)]
    public string? Filename { get; set; }

    /// <summary>
    /// Type of the file, informational only
    /// </summary>
    [Required]
    public string? FileType { get; set; }

    /// <summary>
    /// Size of file
    /// </summary>
    [Required]
    public long Size { get; set; }

    /// <summary>
    /// Path of file
    /// </summary>
    [Required]
    public string? Path { get; set; }

    /// <summary>
    /// Markdown file of the original file, always created as soon as the file is uploaded
    /// </summary>
    [Required]
    public string? MarkdownPath { get; set; }

    /// <summary>
    /// Marks if file is a folder or an independent file
    /// </summary>
    public bool IsFolder { get; set; } = false;

    /// <summary>
    /// Parent folder
    /// </summary>
    public int Parent { get; set; }

    /// <summary>
    /// Foreign key of parent folder
    /// </summary>
    [ForeignKey(nameof(Parent))]
    public virtual File? ParentId { get; set; }
}
