using Microsoft.EntityFrameworkCore;
using Skillbridge.Data;
using Skillbridge.Models;

namespace Skillbridge.Services;

public interface IOrganizationMemberService
{
    Task<OrganizationMember?> GetMemberAsync(string organizationId, string userId);
    Task<List<OrganizationMember>> GetMembersAsync(string organizationId);

    public class OrganizationMemberService(ApplicationDbContext context, IOrganizationService organizationService) : IOrganizationMemberService
    {
        public Task<OrganizationMember?> GetMemberAsync(string organizationId, string userId)
        {
            var member = context.OrganizationMembers
                .Where(om => om.Organization == organizationId && om.User == userId)
                .FirstOrDefaultAsync();
            
            return member;
        }

        public async Task<List<OrganizationMember>> GetMembersAsync(string organizationId)
        {
            var members = await context.OrganizationMembers
                .Where(om => om.Organization == organizationId)
                .ToListAsync();

            return members;
        }
        

    }
}