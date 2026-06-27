using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Data;
using Skillbridge.Models;

namespace Skillbridge.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrganizationController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrganizationController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/organization
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Organization>>> GetOrganizations()
    {
        return await _context.Organizations.ToListAsync();
    }

    // GET: api/organization/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Organization>> GetOrganization(string id)
    {
        var organization = await _context.Organizations.FindAsync(id);

        if (organization == null)
            return NotFound();

        return organization;
    }

    // POST: api/organization
    [HttpPost]
    public async Task<ActionResult<Organization>> CreateOrganization(Organization organization)
    {
        organization.OrganizationId = Guid.NewGuid().ToString();

        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrganization),
            new { id = organization.OrganizationId },
            organization);
    }

    // PUT: api/organization/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrganization(string id, Organization organization)
    {
        if (id != organization.OrganizationId)
            return BadRequest();

        _context.Entry(organization).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Organizations.Any(o => o.OrganizationId == id))
                return NotFound();

            throw;
        }

        return NoContent();
    }

    // DELETE: api/organization/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrganization(string id)
    {
        var organization = await _context.Organizations.FindAsync(id);

        if (organization == null)
            return NotFound();

        _context.Organizations.Remove(organization);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}