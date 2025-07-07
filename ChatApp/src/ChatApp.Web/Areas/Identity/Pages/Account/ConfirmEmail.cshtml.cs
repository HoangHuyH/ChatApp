using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatApp.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace ChatApp.Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<User> _userManager;

        public ConfirmEmailModel(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public bool IsConfirmed { get; set; }
        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                IsConfirmed = false;
                ErrorMessage = "Invalid email confirmation link.";
                return Page();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                IsConfirmed = false;
                ErrorMessage = "Unable to find user.";
                return Page();
            }

            try
            {
                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                var result = await _userManager.ConfirmEmailAsync(user, code);
                
                if (result.Succeeded)
                {
                    IsConfirmed = true;
                    StatusMessage = "Thank you for confirming your email.";
                }
                else
                {
                    IsConfirmed = false;
                    ErrorMessage = "Error confirming your email: " + string.Join(", ", result.Errors.Select(e => e.Description));
                }
            }
            catch (Exception ex)
            {
                IsConfirmed = false;
                ErrorMessage = "Error processing email confirmation: " + ex.Message;
            }

            return Page();
        }
    }
}
