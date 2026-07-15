using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Skillbridge.Data;
using Skillbridge.Models;
using Skillbridge.Models.Project;
using Skillbridge.Services;
using Skillbridge.Utilities;

namespace Skillbridge.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class ProjectController(ApplicationDbContext context, IProjectService projectService) : ControllerBase
{

    /// <summary>
    /// Listar todos os projetos que o utilizador tem acesso
    /// </summary>
    /// <returns>Lista de projetos</returns>
    // GET: api/project
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Project>>> GetAllProjects()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();
        
        var result = await projectService.GetAllProjectAsync(userId);
        if (result.IsNullOrEmpty()) return NotFound();
        
        return Ok(result);
    }
    
    /// <summary>
    /// Lista o projeto requerido
    /// </summary>
    /// <param name="id">Id do projeto</param>
    /// <returns>Retorna projeto</returns>
    // GET: api/project/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Project>> GetProject(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var result = await projectService.GetProjectAsync(userId, id);
        if (result == null) return NotFound();
        
        return Ok(result);
    }
    
    /// <summary>
    /// Criar novo projeto
    /// </summary>
    /// <param name="organizationId">Id da organização</param>
    /// <param name="projectName">Nome do projeto</param>
    /// <param name="projectDescription">Descrição do projeto</param>
    /// <param name="repository">Repositório (opcional)</param>
    /// <param name="isPublic">Se é publico</param>
    /// <returns></returns>
    // POST: api/project
    [HttpPost]
    public async Task<ActionResult<Project>> CreateProject(string organizationId, string projectName, string projectDescription, string? repository, bool isPublic = true)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();
        
        if (string.IsNullOrWhiteSpace(projectName) || string.IsNullOrWhiteSpace(organizationId))  return BadRequest();
        
        var result = await projectService.CreateProjectAsync(organizationId, userId, projectName, projectDescription,
            repository, isPublic);

        if (result.Success) return Ok(result.Message);
        
        switch (result.ErrorType)
        {
            case ErrorType.Denied: return Forbid(result.Message);
            case ErrorType.NotFound: return NotFound(result.Message);
            default: return BadRequest(result.Message);
        }
    }
    
    /// <summary>
    /// Eliminar projeto
    /// </summary>
    /// <param name="id">Id do projeto a eliminar</param>
    /// <returns></returns>
    // DELETE: api/project/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();
        
        var result = await projectService.DeleteProjectAsync(userId, id);

        if (result.Success) return Ok(result.Message);

        switch (result.ErrorType)
        {
            case ErrorType.Denied: return Forbid(result.Message);
            case ErrorType.NotFound: return NotFound(result.Message);
            default: return BadRequest(result.Message);
        }
    }
}