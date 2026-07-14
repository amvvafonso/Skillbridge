using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Data;
using Skillbridge.Models;
using Skillbridge.Services;

namespace Skillbridge.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class OrganizationController(ApplicationDbContext context, IOrganizationService organizationService) : ControllerBase
{
    /// <summary>
    /// Retorna todas as organizações
    /// </summary>
    /// <returns>LÇista de organizações</returns>
    // GET: api/organization
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Organization>>> GetOrganizations()
    {
        return await context.Organizations.ToListAsync();
    }

    /// <summary>
    /// Devolve a organização 
    /// </summary>
    /// <param name="id">Id da organização</param>
    /// <returns>Retorna organização</returns>
    // GET: api/organization/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Organization>> GetOrganization(string id)
    {
        var organization = await context.Organizations.FindAsync(id);

        if (organization == null)
            return NotFound();

        return organization;
    }

    /// <summary>
    /// Criar uma organização
    /// </summary>
    /// <param name="organizationName">Nome</param>
    /// <param name="organizationAddress">Morada</param>
    /// <param name="organizationDescription">Descrição</param>
    /// <param name="logo">Logotipo</param>
    /// <returns>Retorna se a operação foi bem sucedida</returns>
    // POST: api/organization
    [HttpPost]
    public async Task<ActionResult<Organization>> CreateOrganization(string organizationName, string organizationAddress, string organizationDescription, IFormFile? logo)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var result = await organizationService.CreateOrganizationAsync(userId, organizationName, organizationAddress, organizationDescription, logo);

        if (result.Success) return Ok(result);
        
        switch (result.ErrorType)
        {
            case ErrorType.Denied: return Forbid();
            case ErrorType.NotFound: return NotFound();
            default:
                return BadRequest(result.Message);
        }
  
    }
    
    /// <summary>
    /// Atualizar a organização
    /// </summary>
    /// <param name="id">Id da organização</param>
    /// <param name="organizationName">Novo nome</param>
    /// <param name="organizationAddress">Nova mroada</param>
    /// <param name="organizationDescription">Nova descrição</param>
    /// <param name="logo">Novo logotipo</param>
    /// <param name="banner">"Banner" (apenas pode ser alterada no update)</param>
    /// <returns></returns>
    // PUT: api/organization/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrganization(string id, string organizationName, string organizationAddress, string organizationDescription, IFormFile? logo, IFormFile? banner)
    {

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();
        var result = await organizationService.EditOrganizationAsync(id, userId, organizationName, organizationAddress, organizationDescription, logo, banner);

        if (result.Success) return Ok(result.Message);
        
        switch (result.ErrorType)
        {
            case ErrorType.Denied: return Forbid();
            case ErrorType.NotFound: return NotFound();
            default:
                return BadRequest(result.Message);
        }
    }
    
    /// <summary>
    /// Eliminar organização e todos os dados/ficheiros associados
    /// </summary>
    /// <param name="id">Id da organização</param>
    /// <returns></returns>
    // DELETE: api/organization/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrganization(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();
        
        var result = await organizationService.DeleteOrganizationAsync(id, userId);
        
        if (result.Success) return Ok(result.Message);
        
        switch (result.ErrorType)
        {
            case ErrorType.Denied: return Forbid();
            case ErrorType.NotFound: return NotFound();
            default:
                return BadRequest(result.Message);
        }
    }
}