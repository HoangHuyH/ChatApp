using ChatApp.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace ChatApp.Web.Areas.Identity.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly IPasswordResetService _passwordResetService;

        public ResetPasswordModel(IPasswordResetService passwordResetService)
        {
            _passwordResetService = passwordResetService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        public class InputModel
        {
            [Required(ErrorMessage = "Mã xác thực là bắt buộc")]
            [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã xác thực phải có 6 số")]
            public string Code { get; set; } = string.Empty;

            [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
            [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }        public IActionResult OnGet(string? email = null)
        {
            if (email == null)
            {
                return BadRequest("Email is required");
            }

            Email = email;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var result = await _passwordResetService.ResetPasswordAsync(Email, Input.Code, Input.Password);
            
            if (result)
            {
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            ModelState.AddModelError(string.Empty, "Mã xác thực không hợp lệ hoặc đã hết hạn");
            return Page();
        }
    }
}
