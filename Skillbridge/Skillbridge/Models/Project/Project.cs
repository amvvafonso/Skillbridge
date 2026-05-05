using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Skillbridge.Models.Project;

public class Project
{
    /// <summary>
    /// Identifier of project
    /// </summary>
    [Key]
    public int ProjectId { get; set; }
    
    /// <summary>
    /// Name of the project
    /// </summary>
    [Required(ErrorMessage = "Project name is required")]
    [MaxLength(200)]
    public string? ProjectName { get; set; }
    
    /// <summary>
    /// Description of project, small introduction
    /// </summary>
    [Required(ErrorMessage = "Project description is required")]
    [MaxLength(200)]
    public string? ProjectDescription { get; set; }
    
    /// <summary>
    /// GitHub link of the project, optional
    /// </summary>
    public string? Repository { get; set; }

    /// <summary>
    /// Determines if project is public or private
    /// </summary>
    public bool Public { get; set; } = true;
    
    /// <summary>
    /// Directory of project, where the files are stored
    /// </summary>
    [Required(ErrorMessage = "Project directory is required")]
    public string? ProjectDirectory { get; set; }
    
    /// <summary>
    /// Foreign key, linked to the organization id
    /// </summary>
    public int OrganizationId { get; set; }

    [ForeignKey(nameof(OrganizationId))] 
    public virtual Organization Organization { get; set; } = null!;


    // ############################################################
    // Relacionamentos M-N
    // ############################################################
    public ICollection<UserProjectAccess> UserProjectAccessList { get; set; } = []; 
    
    // ############################################################
    // Relacionamentos 1-N
    // ############################################################
    public ICollection<File> FileList { get; set; } = [];
}