using System.Threading.Tasks;

namespace ChatApp.Web.Services
{
    public interface IEmailService
    {
        Task<bool> SendPasswordResetEmailAsync(string email, string resetCode);
        Task<bool> SendEmailChangeConfirmationAsync(string email, string confirmationCode);
    }
}
