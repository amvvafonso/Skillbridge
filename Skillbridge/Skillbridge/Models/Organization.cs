using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Models.Client;

namespace Skillbridge.Models;

/// <summary>
/// Classe modelo da organização
/// </summary>
public class Organization
{
    /// <summary>
    /// Id da organização
    /// </summary>
    [Key]
    public string OrganizationId { get; set; }
    
    /// <summary>
    /// Nome da organização
    /// </summary>
    [Required(ErrorMessage = "Organization name is required")]
    [MaxLength(100)]
    public string OrganizationName { get; set; } = string.Empty;
    
    /// <summary>
    /// Morada da organização
    /// </summary>
    [MaxLength(200)]
    [Required(ErrorMessage = "Organization address is required")]
    public string OrganizationAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Descrição da organização (max 1000)
    /// </summary>
    [MaxLength(1000)]
    [Required(ErrorMessage = "Organization description is required")]
    public string OrganizationDescription { get; set; }  = string.Empty;
    
    /// <summary>
    /// Path para o logo
    /// </summary>
    [MaxLength(1000)]
    public string LogoPath { get; set; } = "/default_logo.png";
    
    /// <summary>
    /// Path para a banner
    /// </summary>
    [MaxLength(1000)]
    public string BannerPath { get; set; } = "/default_banner.png";
    
    /// <summary>
    /// Foreign key, liga ao dono 
    /// </summary>
    public string Owner { get; set; }
    
    [ForeignKey(nameof(Owner))]
    public virtual User? OrganizationOwner { get; set; } = null!;
}