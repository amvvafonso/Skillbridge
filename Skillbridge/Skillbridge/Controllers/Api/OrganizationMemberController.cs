using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Data;
using Skillbridge.Models;

namespace Skillbridge.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrganizationMemberController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrganizationMemberController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/organizationmember
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrganizationMember>>> GetMembers()
    {
        return await _context.OrganizationMembers
            .Include(m => m.IdOrganization)
            .Include(m => m.IdUser)
            .ToListAsync();
    }

    // GET: api/organizationmember/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<OrganizationMember>> GetMember(string id)
    {
        var member = await _context.OrganizationMembers
            .Include(m => m.IdOrganization)
            .Include(m => m.IdUser)
            .FirstOrDefaultAsync(m => m.OrganizationMemberId == id);

        if (member == null)
            return NotFound();

        return member;
    }

    // GET: api/organizationmember/organization/{organizationId}
    [HttpGet("organization/{organizationId}")]
    public async Task<ActionResult<IEnumerable<OrganizationMember>>> GetMembersByOrganization(string organizationId)
    {
        return await _context.OrganizationMembers
            .Where(m => m.Organization == organizationId)
            .Include(m => m.IdUser)
            .ToListAsync();
    }

    // POST: api/organizationmember
    [HttpPost]
    public async Task<IActionResult> CreateMember(OrganizationMember member)
    {
        member.OrganizationMemberId = Guid.NewGuid().ToString();

        _context.OrganizationMembers.Add(member);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMember),
            new { id = member.OrganizationMemberId },
            member);
    }

    // PUT: api/organizationmember/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMember(string id, OrganizationMember member)
    {
        if (id != member.OrganizationMemberId)
            return BadRequest();

        _context.Entry(member).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.OrganizationMembers.AnyAsync(m => m.OrganizationMemberId == id))
                return NotFound();

            throw;
        }

        return NoContent();
    }

    // DELETE: api/organizationmember/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMember(string id)
    {
        var member = await _context.OrganizationMembers.FindAsync(id);

        if (member == null)
            return NotFound();

        _context.OrganizationMembers.Remove(member);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}