using Microsoft.EntityFrameworkCore;
using Skillbridge.Data;
using Skillbridge.Models;

namespace Skillbridge.Services;

/// <summary>
/// Serviço responsável pela consulta de membros de organizações
/// </summary>
public interface IOrganizationMemberService
{
    /// <summary>
    /// Obtém o registo de membro de um user numa organização específica
    /// </summary>
    /// <param name="organizationId">Identificador da organização</param>
    /// <param name="userId">Identificador do user</param>
    /// <returns>O <see cref="OrganizationMember"/> correspondente, ou <c>null</c> se não for membro</returns>
    Task<OrganizationMember?> GetMemberAsync(string organizationId, string userId);
    
    /// <summary>
    /// Obtém todos os membros pertencentes a uma organização
    /// </summary>
    /// <param name="organizationId">Identificador da organização</param>
    /// <returns>Lista de <see cref="OrganizationMember"/> da organização</returns>
    Task<List<OrganizationMember>> GetMembersAsync(string organizationId);


    /// <inheritdoc />
    public class OrganizationMemberService(ApplicationDbContext context, IOrganizationService organizationService) : IOrganizationMemberService
    {
        /// <inheritdoc />
        public Task<OrganizationMember?> GetMemberAsync(string organizationId, string userId)
        {
            var member = context.OrganizationMembers
                .Where(om => om.Organization == organizationId && om.User == userId)
                .FirstOrDefaultAsync();
            
            return member;
        }

        /// <inheritdoc />
        public async Task<List<OrganizationMember>> GetMembersAsync(string organizationId)
        {
            var members = await context.OrganizationMembers
                .Where(om => om.Organization == organizationId)
                .ToListAsync();

            return members;
        }
        

    }
}