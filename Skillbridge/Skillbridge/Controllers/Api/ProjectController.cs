using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Data;
using Skillbridge.Models.Project;

namespace Skillbridge.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProjectController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/project
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
    {
        return await _context.Project
            .Include(p => p.Organization)
            .ToListAsync();
    }

    // GET: api/project/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Project>> GetProject(int id)
    {
        var project = await _context.Project
            .Include(p => p.Organization)
            .Include(p => p.UserProjectAccessList)
            .FirstOrDefaultAsync(p => p.ProjectId == id);

        if (project == null)
            return NotFound();

        return project;
    }

    // GET: api/project/organization/{organizationId}
    [HttpGet("organization/{organizationId}")]
    public async Task<ActionResult<IEnumerable<Project>>> GetProjectsByOrganization(string organizationId)
    {
        return await _context.Project
            .Where(p => p.OrganizationId == organizationId)
            .Include(p => p.Organization)
            .ToListAsync();
    }

    // POST: api/project
    [HttpPost]
    public async Task<ActionResult<Project>> CreateProject(Project project)
    {
        _context.Project.Add(project);
        await _context.SaveChangesAsync();

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

        _context.Entry(project).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Project.AnyAsync(p => p.ProjectId == id))
                return NotFound();

            throw;
        }

        return NoContent();
    }

    // DELETE: api/project/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var project = await _context.Project.FindAsync(id);

        if (project == null)
            return NotFound();

        _context.Project.Remove(project);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}