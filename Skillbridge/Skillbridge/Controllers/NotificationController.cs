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
public class NotificationController(ApplicationDbContext context, INotificationService notificationService) : Controller
{
  
    [HttpPost]
    public async Task<IActionResult> AcceptInvite([FromForm] string notificationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();
        
        var result = await notificationService.AcceptOrganizationInviteAsync(notificationId, userId);
        
        if (result.Success)
        {
            ToastHelper.ShowToast(TempData, "Sucesso", result.Message, "success");
            return LocalRedirect("/");
        }
        
        ToastHelper.ShowToast(TempData, "Erro", result.Message, "warning");
        return LocalRedirect("/");
    }

    
    [HttpPost]
    public async Task<IActionResult> Decline([FromForm] string notificationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var result = await notificationService.DeclineOrganizationInviteAsync(notificationId, userId);
        
        if (result.Success)
        {
            ToastHelper.ShowToast(TempData, "Sucesso", result.Message, "success");
            return LocalRedirect("/");
        }
        
        ToastHelper.ShowToast(TempData, "Erro", result.Message, "warning");
        return LocalRedirect("/");
    }
    
}