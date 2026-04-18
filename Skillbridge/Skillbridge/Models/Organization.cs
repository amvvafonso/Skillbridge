using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;

namespace Skillbridge.Models;

public class Organization
{
    /// <summary>
    /// Identifier of the organization (PK)
    /// </summary>
    [Key]
    public int OrganizationId { get; set; }
    
    /// <summary>
    /// Name of organization
    /// </summary>
    [Required(ErrorMessage = "Organization name is required")]
    [MaxLength(100)]
    public string? OrganizationName { get; set; }
    
    /// <summary>
    /// Address of organization
    /// </summary>
    [MaxLength(200)]
    [Required(ErrorMessage = "Organization address is required")]
    public string? OrganizationAddress { get; set; }
    
    /// <summary>
    /// Description of the organization, max 1000 chars 
    /// </summary>
    [MaxLength(1000)]
    public string? OrganizationDescription { get; set; }
    
    /// <summary>
    /// Foreign key, link to owner of organization
    /// </summary>
    
    public int Owner { get; set; }

    [ForeignKey(nameof(Owner))]
    public virtual User? OrganizationOwner { get; set; } = null!;
}