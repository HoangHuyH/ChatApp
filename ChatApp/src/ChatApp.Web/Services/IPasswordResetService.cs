using System.Threading.Tasks;

namespace ChatApp.Web.Services
{
    public interface IPasswordResetService
    {
        Task<bool> SendPasswordResetCodeAsync(string email);
        Task<bool> ValidateResetCodeAsync(string email, string code);
        Task<bool> ResetPasswordAsync(string email, string code, string newPassword);
        Task CleanupExpiredTokensAsync();
    }
}
