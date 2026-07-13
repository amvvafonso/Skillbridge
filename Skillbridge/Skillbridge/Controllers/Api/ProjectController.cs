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
    
    // POST: api/project
    [HttpPost]
    public async Task<ActionResult<Project>> CreateProject(string organizationId, string userId, string projectName, string projectDescription, string? repository, bool isPublic = true)
    {
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