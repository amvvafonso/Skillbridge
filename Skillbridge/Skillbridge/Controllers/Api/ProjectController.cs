using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Skillbridge.Data;
using Skillbridge.Models.Project;
using Skillbridge.Services;

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

    // GET: api/project/organization/{organizationId}
    [HttpGet("organization/{organizationId}")]
    public async Task<ActionResult<IEnumerable<Project>>> GetProjectsByOrganization(string organizationId)
    {
        return await context.Project
            .Where(p => p.OrganizationId == organizationId)
            .Include(p => p.Organization)
            .ToListAsync();
    }

    // POST: api/project
    [HttpPost]
    public async Task<ActionResult<Project>> CreateProject(Project project)
    {
        context.Project.Add(project);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProject),
            new { id = project.ProjectId },
            project);
    }

    // PUT: api/project/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProject(int id, Project project)
    {
        if (id != project.ProjectId)
            return BadRequest();

        context.Entry(project).State = EntityState.Modified;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await context.Project.AnyAsync(p => p.ProjectId == id))
                return NotFound();

            throw;
        }

        return NoContent();
    }

    // DELETE: api/project/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var project = await context.Project.FindAsync(id);

        if (project == null)
            return NotFound();

        context.Project.Remove(project);
        await context.SaveChangesAsync();

        return NoContent();
    }
}