using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using ChatApp.Web.Data;
using ChatApp.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Web.Areas.Identity.Pages.Account.Manage
{
    public class ChangeUsernameModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<ChangeUsernameModel> _logger;
        private readonly ApplicationDbContext _context;

        public ChangeUsernameModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ILogger<ChangeUsernameModel> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "New username (email)")]
            public string NewUsername { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Current password")]
            public string Password { get; set; }
        }

        private async Task LoadAsync(User user)
        {
            var userName = await _userManager.GetUserNameAsync(user);

            Input = new InputModel
            {
                NewUsername = userName
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var currentUsername = await _userManager.GetUserNameAsync(user);
            if (Input.NewUsername == currentUsername)
            {
                StatusMessage = "Your username was not changed (new username is the same as current one).";
                return RedirectToPage();
            }

            // Check if the new username is already taken
            var existingUser = await _userManager.FindByNameAsync(Input.NewUsername);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "Username is already taken.");
                await LoadAsync(user);
                return Page();
            }

            // Verify the current password
            var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, Input.Password);
            if (!isPasswordCorrect)
            {
                ModelState.AddModelError(string.Empty, "Incorrect password.");
                await LoadAsync(user);
                return Page();
            }

            // Change the username
            var setUsernameResult = await _userManager.SetUserNameAsync(user, Input.NewUsername);

            if (!setUsernameResult.Succeeded)
            {
                foreach (var error in setUsernameResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await LoadAsync(user);
                return Page();
            }

            // Also update the email since we use email as username
            var setEmailResult = await _userManager.SetEmailAsync(user, Input.NewUsername);
            if (!setEmailResult.Succeeded)
            {
                foreach (var error in setEmailResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                // Try to revert username change
                await _userManager.SetUserNameAsync(user, currentUsername);
                await LoadAsync(user);
                return Page();
            }

            // Sign in again with new username
            await _signInManager.SignOutAsync();
            await _signInManager.SignInAsync(user, isPersistent: false);

            _logger.LogInformation("User changed their username successfully.");
            StatusMessage = "Your username has been changed.";

            return RedirectToPage();
        }
    }
}