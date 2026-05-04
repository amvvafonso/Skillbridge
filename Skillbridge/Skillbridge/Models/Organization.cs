using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Models.Client;

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
    /// Path to logo
    /// </summary>
    [MaxLength(1000)]
    public string LogoPath { get; set; } = "/default_logo.png";
    
    /// <summary>
    /// Path to banner
    /// </summary>
    [MaxLength(1000)]
    public string BannerPath { get; set; } = "/default_banner.png";
    
    /// <summary>
    /// Foreign key, link to owner of organization
    /// </summary>
    public string Owner { get; set; }
    
    [ForeignKey(nameof(Owner))]
    public virtual User? OrganizationOwner { get; set; } = null!;
}