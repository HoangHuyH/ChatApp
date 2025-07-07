using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace ChatApp.Web.Services
{
    public class NoOpEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // No-op implementation
            return Task.CompletedTask;
        }
    }
}
