using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Skillbridge.Data;
using Skillbridge.Models.Project;
using Skillbridge.Models.Client;
using Microsoft.AspNetCore.Identity;


namespace Skillbridge.Areas.Client.Pages 
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        
        // Injeçao das dependencias do construtor
        public IndexModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        
        public User CurrentUser { get; set; } 
        public List<Session> Sessions { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            CurrentUser = await _userManager.GetUserAsync(User);
            Sessions = _context.Sessions.ToList();

            if (CurrentUser != null)
            {
                var nome = CurrentUser.Name;
            }
            
            return Page();
        }
    }
}

