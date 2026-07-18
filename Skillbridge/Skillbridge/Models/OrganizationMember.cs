using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Data;
using Skillbridge.Models.Client;

namespace Skillbridge.Models;

/// <summary>
/// Classe modelo do membro da organização
/// </summary>
public class OrganizationMember(string organizationMemberId, string organization, string user, Role role)
{
    /// <summary>
    /// PK, Id do registo
    /// </summary>
    [Key]
    public string OrganizationMemberId { get; set; } = organizationMemberId;

    /// <summary>
    /// Role do membro
    /// </summary>
    public Role Role { get; set; } = role;

    /// <summary>
    /// Foreign key, liga à organização
    /// </summary>
    public string Organization { get; set; } = organization;

    [ForeignKey(nameof(Organization))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public virtual Organization? IdOrganization { get; set; }
    
    /// <summary>
    /// Foreign key, aponta para o utilizador
    /// </summary>
    public string User { get; set; } = user;

    [ForeignKey(nameof(User))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public virtual User? IdUser { get; set; }

    /// <summary>
    /// Devolve se o utilizador é membro de uma determinada organização
    /// </summary>
    /// <param name="context"></param>
    /// <param name="memberEmail"></param>
    /// <param name="organizationId"></param>
    /// <returns></returns>
    public static async Task<bool> IsMember(ApplicationDbContext context, string memberEmail, string organizationId)
    {
        return await context.OrganizationMembers
            .AnyAsync(om => om.Organization == organizationId && om.IdUser != null && om.IdUser.Email == memberEmail);
    }
    
}