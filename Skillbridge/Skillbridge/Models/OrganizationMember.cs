using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Data;
using Skillbridge.Models.Client;

namespace Skillbridge.Models;

public class OrganizationMember
{
    public OrganizationMember(string organizationMemberId ,string organization, string user, Role role)
    {
        OrganizationMemberId = organizationMemberId;
        Organization = organization;
        User = user;
        Role = role;
    }


    /// <summary>
    /// PK, Identifier of member
    /// </summary>
    [Key]
    public string OrganizationMemberId { get; set; }
    
    /// <summary>
    /// Role of member
    /// </summary>
    public Role Role { get; set; }
    
    /// <summary>
    /// Foreign key, points to organization
    /// </summary>
    public string Organization { get; set; }
    [ForeignKey(nameof(Organization))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public virtual Organization? IdOrganization { get; set; }
    
    /// <summary>
    /// Foreign key, points to user
    /// </summary>
    public string User { get; set; }
    [ForeignKey(nameof(User))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public virtual User? IdUser { get; set; }

    public static async Task<bool> IsMember(ApplicationDbContext context, string memberEmail, string organizationId)
    {
        return await context.OrganizationMembers
            .AnyAsync(om => om.Organization == organizationId && om.IdUser.Email == memberEmail);
    }
    
}