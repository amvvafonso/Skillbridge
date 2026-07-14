using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Data;
using Skillbridge.Models;
using Skillbridge.Models.Client;
using Skillbridge.Models.Project;
using Skillbridge.Services;
using Skillbridge.Utilities;

namespace Skillbridge.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionController(ApplicationDbContext context, ISessionService sessionService) : ControllerBase
{
  
    // GET: api/session
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Session>>> GetSessions()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();
        
        return await sessionService.GetAllSessionsAsync(userId);
    }

    // GET: api/session/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Session?>> GetSession(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        return await sessionService.GetSessionAsync(id);
    }
    

    // POST: api/session
    [HttpPost]
    public async Task<ActionResult<string>> CreateSession(string bucket, string key, string title, string description, bool isPublic)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var result = await sessionService.CreateSessionAsync(bucket, key, title, description, isPublic, userId);
        if (result.Success) return Ok(result.Message);

        switch (result.ErrorType)
        {
            case ErrorType.Denied: return Forbid();
            case ErrorType.NotFound: return NotFound();
            default:
                return BadRequest(result.Message);
        }
    }


    
    [HttpPost("session/invite/{sessionId}")]
    public async Task<ActionResult<string>> InviteMember(string sessionId, string userEmail, Role role)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var result = await sessionService.AllowEntrance(sessionId, userEmail, userId, role);
        
        if (result.Success) return Ok(result.Message);

        switch (result.ErrorType)
        {
            case ErrorType.Denied: return Forbid();
            case ErrorType.NotFound: return NotFound(result.Message);
            default:
                return BadRequest(result.Message);
        }

    }

    [HttpPost("session/end/{sessionId}")]
    public async Task<ActionResult<string>> EndSession(string sessionId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();
        
        var result = await sessionService.EndSessionAsync(sessionId, userId);
        
        if (result.Success) return Ok(result.Message);

        switch (result.ErrorType)
        {
            case ErrorType.Denied: return Forbid();
            case ErrorType.NotFound: return NotFound(result.Message);
            default:
                return BadRequest(result.Message);
        }
    }

}