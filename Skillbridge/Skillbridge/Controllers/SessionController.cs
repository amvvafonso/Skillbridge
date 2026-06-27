using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Data;
using Skillbridge.Models.Project;

namespace Skillbridge.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SessionController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/session
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Session>>> GetSessions()
    {
        return await _context.Sessions
            .Include(s => s.file)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    // GET: api/session/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Session>> GetSession(string id)
    {
        var session = await _context.Sessions
            .Include(s => s.file)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null)
            return NotFound();

        return session;
    }

    // GET: api/session/file/{fileId}
    [HttpGet("file/{fileId}")]
    public async Task<ActionResult<IEnumerable<Session>>> GetSessionsByFile(string fileId)
    {
        return await _context.Sessions
            .Where(s => s.fileId == fileId)
            .Include(s => s.file)
            .ToListAsync();
    }

    // POST: api/session
    [HttpPost]
    public async Task<ActionResult<Session>> CreateSession(Session session)
    {
        session.Id = Guid.NewGuid().ToString();
        session.CreatedAt = DateTime.UtcNow;

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSession),
            new { id = session.Id },
            session);
    }

    // PUT: api/session/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSession(string id, Session session)
    {
        if (id != session.Id)
            return BadRequest();

        _context.Entry(session).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Sessions.AnyAsync(s => s.Id == id))
                return NotFound();

            throw;
        }

        return NoContent();
    }

    // DELETE: api/session/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSession(string id)
    {
        var session = await _context.Sessions.FindAsync(id);

        if (session == null)
            return NotFound();

        _context.Sessions.Remove(session);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}