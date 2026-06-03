using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Data; // Ajuste para o seu DbContext
using Skillbridge.Models.Client;
using System.Linq;
using System.Threading.Tasks;

namespace Skillbridge.ViewComponents
{
    public class NotificationMenuViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public NotificationMenuViewComponent(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.GetUserAsync((System.Security.Claims.ClaimsPrincipal)User);
            
            if (user == null)
            {
                return View(new List<Skillbridge.Models.Notification>());
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.Date)
                .Take(5)
                .ToListAsync();

            return View(notifications);
        }
    }
}