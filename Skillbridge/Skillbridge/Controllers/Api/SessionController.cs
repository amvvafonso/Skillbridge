using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Skillbridge.Data;
using Skillbridge.Models;
using Skillbridge.Models.Client;
using Skillbridge.Models.Project;
using Skillbridge.Services;

namespace Skillbridge.Controllers.Api;

/// <inheritdoc />
[ApiController]
[Route("api/[controller]")]
public class SessionController(ApplicationDbContext context, ISessionService sessionService) : ControllerBase
{
  
    /// <summary>
    /// Listar todas as sessões que o utilizador tem acesso
    /// </summary>
    /// <returns>Lista de sessões (ativas!)</returns>
    // GET: api/session
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Session>>> GetSessions()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();
        
        return await sessionService.GetAllSessionsAsync(userId);
    }

    /// <summary>
    /// Vai buscar a sessão especificada
    /// </summary>
    /// <param name="id">Id da sessão</param>
    /// <returns>Sessão</returns>
    // GET: api/session/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Session?>> GetSession(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        return await sessionService.GetSessionAsync(id);
    }
    
    /// <summary>
    /// Criar uma sessão
    /// </summary>
    /// <param name="bucket">Bucket</param>
    /// <param name="key">Key do ficheiro</param>
    /// <param name="title">Nome da sessão</param>
    /// <param name="description">Descrição da sessão</param>
    /// <param name="isPublic">Pública</param>
    /// <returns></returns>
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


    /// <summary>
    /// Convidar utilizador para a sessão
    /// </summary>
    /// <param name="sessionId">Id da sessão</param>
    /// <param name="userEmail">Email do utilizador a adicionar</param>
    /// <param name="role">A sua role (1 - Mentor, 2 - Apprentice, 3 - Unknown, 4 - Manager, 5 - Owner)</param>
    /// <returns></returns>
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
    
    /// <summary>
    /// Terminar sessão ativa
    /// </summary>
    /// <param name="sessionId">Id da sessão</param>
    /// <returns></returns>
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