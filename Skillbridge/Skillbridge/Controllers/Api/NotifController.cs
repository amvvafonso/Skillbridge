using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol;
using Skillbridge.Data;
using Skillbridge.Models;
using Skillbridge.Models.Client;
using Skillbridge.Services;
using Skillbridge.Utilities;

namespace Skillbridge.Controllers;

[Authorize]
public class NotifController(ApplicationDbContext context, INotificationService notificationService) : ControllerBase
{
    
    /// <summary>
    /// Aceita o convite para a organização
    /// </summary>
    /// <param name="notificationId">Id da organização</param>
    /// <returns></returns>
    [HttpPost("/accept/{notificationId}")]
    public async Task<IActionResult> AcceptInvite(string notificationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();
        
        var result = await notificationService.AcceptOrganizationInviteAsync(notificationId, userId);
        
        if (result.Success) return Ok(result.Message);
        
        return BadRequest(result.Message);
    }

    /// <summary>
    /// Recusa convite para a organização
    /// </summary>
    /// <param name="notificationId">Id da organização</param>
    /// <returns></returns>
    [HttpPost("/decline/{notificationId}")]
    public async Task<IActionResult> Decline(string notificationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var result = await notificationService.DeclineOrganizationInviteAsync(notificationId, userId);
        
        if (result.Success) return Ok(result.Message);
        
        return BadRequest(result.Message);
    }

    /// <summary>
    /// Lista todas as notificações de um utilizador
    /// </summary>
    /// <returns></returns>
    [HttpGet("/notification")]
    public async Task<ActionResult<List<Notification>>> GetNotifications()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var result = await notificationService.GetNotificationAsync(userId);
        
        return Ok(result);
    }
}