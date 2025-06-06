using ChatApp.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace ChatApp.Web.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly IPasswordResetService _passwordResetService;

        public ForgotPasswordModel(IPasswordResetService passwordResetService)
        {
            _passwordResetService = passwordResetService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Email là bắt buộc")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            public string Email { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var result = await _passwordResetService.SendPasswordResetCodeAsync(Input.Email);
                
                if (result)
                {
                    return RedirectToPage("./ResetPassword", new { email = Input.Email });
                }
                
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi gửi email. Vui lòng thử lại.");
            }

            return Page();
        }
    }
}
