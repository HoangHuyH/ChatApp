using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using ChatApp.Web.Models.Entities;
using ChatApp.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace ChatApp.Web.Areas.Identity.Pages.Account.Manage
{
    public class EmailModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailModel> _logger;

        public EmailModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IEmailService emailService,
            ILogger<EmailModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _logger = logger;
        }

        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        public bool IsEmailConfirmed { get; set; }

        [TempData]
        public string StatusMessage { get; set; } = "";

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email mới")]
            public string NewEmail { get; set; } = "";

            [Display(Name = "Mã xác thực")]
            public string ConfirmationCode { get; set; } = "";
        }

        private async Task LoadAsync(User user)
        {
            var email = await _userManager.GetEmailAsync(user);
            Email = email ?? "";

            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);

            Input = new InputModel
            {
                NewEmail = email ?? "",
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Không thể tải user với ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Không thể tải user với ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var email = await _userManager.GetEmailAsync(user);
            if (Input.NewEmail != email)
            {
                // Generate confirmation code
                var confirmationCode = GenerateConfirmationCode();
                
                // Store the confirmation code and new email temporarily
                TempData["PendingEmail"] = Input.NewEmail;
                TempData["ConfirmationCode"] = confirmationCode;
                TempData["CodeExpiry"] = DateTime.UtcNow.AddMinutes(15).ToString();
                
                // Send confirmation email
                var emailSent = await _emailService.SendEmailChangeConfirmationAsync(Input.NewEmail, confirmationCode);
                
                if (emailSent)
                {
                    StatusMessage = "Email xác thực thay đổi email đã được gửi. Vui lòng kiểm tra email của bạn và nhập mã xác thực.";
                }
                else
                {
                    StatusMessage = "Lỗi: Không thể gửi email xác thực.";
                }
            }
            else
            {
                StatusMessage = "Email của bạn không thay đổi.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSendVerificationEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Không thể tải user với ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // Generate confirmation code
            var confirmationCode = GenerateConfirmationCode();
            
            // Store the confirmation code temporarily
            TempData["ConfirmationCode"] = confirmationCode;
            TempData["CodeExpiry"] = DateTime.UtcNow.AddMinutes(15).ToString();
            
            // Send verification email
            var email = await _userManager.GetEmailAsync(user);
            var emailSent = await _emailService.SendEmailVerificationAsync(email, confirmationCode);
            
            if (emailSent)
            {
                StatusMessage = "Email xác thực đã được gửi. Vui lòng kiểm tra email của bạn và nhập mã xác thực.";
            }
            else
            {
                StatusMessage = "Lỗi: Không thể gửi email xác thực.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostConfirmEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Không thể tải user với ID '{_userManager.GetUserId(User)}'.");
            }

            if (string.IsNullOrEmpty(Input.ConfirmationCode))
            {
                ModelState.AddModelError("Input.ConfirmationCode", "Vui lòng nhập mã xác thực.");
                await LoadAsync(user);
                return Page();
            }

            // Check if this is for email change or email verification
            var pendingEmail = TempData["PendingEmail"] as string;
            var storedCode = TempData["ConfirmationCode"] as string;
            var expiryString = TempData["CodeExpiry"] as string;

            // Check if code has expired
            if (DateTime.TryParse(expiryString, out var expiry) && DateTime.UtcNow > expiry)
            {
                ModelState.AddModelError("Input.ConfirmationCode", "Mã xác thực đã hết hạn. Vui lòng gửi mã mới.");
                await LoadAsync(user);
                return Page();
            }
            
            if (Input.ConfirmationCode != storedCode)
            {
                ModelState.AddModelError("Input.ConfirmationCode", "Mã xác thực không đúng.");
                
                // Keep the data for retry
                TempData["PendingEmail"] = pendingEmail;
                TempData["ConfirmationCode"] = storedCode;
                TempData["CodeExpiry"] = expiryString;
                
                await LoadAsync(user);
                return Page();
            }

            if (!string.IsNullOrEmpty(pendingEmail))
            {
                // This is for email change
                var setEmailResult = await _userManager.SetEmailAsync(user, pendingEmail);
                if (!setEmailResult.Succeeded)
                {
                    StatusMessage = "Lỗi: Không thể thay đổi email.";
                    await LoadAsync(user);
                    return Page();
                }

                var setUserNameResult = await _userManager.SetUserNameAsync(user, pendingEmail);
                if (!setUserNameResult.Succeeded)
                {
                    StatusMessage = "Lỗi: Không thể thay đổi username.";
                    await LoadAsync(user);
                    return Page();
                }

                // Refresh sign in
                await _signInManager.RefreshSignInAsync(user);
                StatusMessage = "Email đã được thay đổi thành công.";
            }
            else
            {
                // This is for email verification
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmEmailResult = await _userManager.ConfirmEmailAsync(user, token);
                if (!confirmEmailResult.Succeeded)
                {
                    StatusMessage = "Lỗi: Không thể xác thực email.";
                    await LoadAsync(user);
                    return Page();
                }

                StatusMessage = "Email đã được xác thực thành công.";
            }

            return RedirectToPage();
        }

        private string GenerateConfirmationCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}
