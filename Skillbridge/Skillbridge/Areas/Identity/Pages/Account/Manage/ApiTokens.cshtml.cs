using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Skillbridge.Models;
using Skillbridge.Models.Client;
using Skillbridge.Services;

namespace Skillbridge.Areas.Identity.Pages.Account.Manage
{
    public class ApiTokensModel(IApiTokenService tokenService, UserManager<User> userManager) : PageModel
    {

        public string NewlyCreatedToken { get; set; } = string.Empty;

        public List<ApiToken> Tokens { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "O nome do token é obrigatório.")]
            [MaxLength(100)]
            [Display(Name = "Nome do token")]
            public string TokenName { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Não foi possível encontrar o utilizador com o ID '{userManager.GetUserId(User)}'.");

            Tokens = await tokenService.GetUserTokensAsync(user.Id);
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync([FromForm] string tokenName)
        {
            Console.WriteLine(tokenName);
            var user = await userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Não foi possível encontrar o utilizador com o ID '{userManager.GetUserId(User)}'.");
            

            var result = await tokenService.CreateTokenAsync(user.Id, tokenName);

            if (result.ErrorType.Equals(ErrorType.Denied)) return Forbid();

            
            NewlyCreatedToken = result.Additional;
            Tokens = await tokenService.GetUserTokensAsync(user.Id);
            return Page();
        }

        public async Task<IActionResult> OnPostRevokeAsync(int tokenId)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Não foi possível encontrar o utilizador com o ID '{userManager.GetUserId(User)}'.");

            await tokenService.RevokeTokenAsync(tokenId, user.Id);
            
            TempData["Message"] = "Token revoked.";
            return RedirectToPage();
        }
    }
}
