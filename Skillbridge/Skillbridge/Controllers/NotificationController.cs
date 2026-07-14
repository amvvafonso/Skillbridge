using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Data;
using Skillbridge.Models;
using Skillbridge.Models.Client;
using Skillbridge.Utilities;

namespace Skillbridge.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    
    public NotificationController(ApplicationDbContext context,  UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    [HttpPost]
    public async Task<IActionResult> AcceptInvite(string notificationId, string returnUrl)
    {
        try
        {
            // Gets the notification content
            var notif = await _context.Notifications.FindAsync(notificationId);
            if (notif == null)
                return BadRequest();
            
            // Param represents the key value to proces the notification, e.g OrganizationInvite => param=OrganizationId
            string param = notif.Param;
            
            // Switches between the different types of notifications
            switch (notif.Type)
            {   
                case NotificationType.OrganizationInvite:
                    // Fetches user Id
                    var userId = _userManager.GetUserId(User);
                    
                    // Checks if member is already on the organization
                    var alreadyMember = await _context.OrganizationMembers
                        .AnyAsync(m => m.Organization == param && m.User == userId);
    
                    if (!alreadyMember)
                    {
                        // Inserts into db the organization/member relation
                        await _context.OrganizationMembers.AddAsync(new OrganizationMember(Guid.NewGuid().ToString(),param, userId, Role.Apprentice));
                    }
                    
                    // Updates notification
                    notif.Hidden = true;
                    
                    // Saves changes to DB
                    await _context.SaveChangesAsync(); 
                    
                    // Toast, lets the user know it went sucessfuly
                    ToastHelper.ShowToast(TempData, "Sucesso", "O convite foi aceite com sucesso!", "success");
                    break;
            }

            
        }
        catch (Exception e)
        {
            ToastHelper.ShowToast(TempData, "Erro", e.Message, "error");
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        var referer = Request.Headers["Referer"].ToString();
        if (!string.IsNullOrEmpty(referer))
            return Redirect(referer);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Decline(string notificationId, string returnUrl)
    {
        try
        {
            var notif = await _context.Notifications.FindAsync(notificationId);
            if (notif != null)
            {
                notif.Hidden = true;
                _context.Notifications.Update(notif);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        if (!string.IsNullOrEmpty(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }
}