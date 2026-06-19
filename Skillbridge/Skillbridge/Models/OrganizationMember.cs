using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Models.Client;

namespace Skillbridge.Models;

public class OrganizationMember
{
    public OrganizationMember(string organizationMemberId ,string organization, string user)
    {
        OrganizationMemberId = organizationMemberId;
        Organization = organization;
        User = user;
    }


    /// <summary>
    /// PK, Identifier of member
    /// </summary>
    [Key]
    public string OrganizationMemberId { get; set; }
    
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
    
}