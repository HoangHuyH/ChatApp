using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChatApp.Web.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _apiToken;
        private readonly string _senderEmail;

        public EmailService(
            ILogger<EmailService> logger,
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
            _apiToken = _configuration["MAILERSEND_API_TOKEN"] ?? "";
            _senderEmail = _configuration["MAILERSEND_SENDER_EMAIL"] ?? "info@domain.com";
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string resetCode)
        {
            if (string.IsNullOrEmpty(_apiToken))
            {
                _logger.LogWarning("MailerSend API token không được cấu hình.");
                return false;
            }

            var subject = "Đặt lại mật khẩu - ChatApp";
            var htmlContent = $"<h2>Đặt lại mật khẩu - ChatApp</h2><p>Mã xác thực của bạn: <strong>{resetCode}</strong></p><p>Mã này có hiệu lực trong 15 phút.</p>";
            var textContent = $"Mã xác thực đặt lại mật khẩu: {resetCode}. Có hiệu lực trong 15 phút.";

            return await SendEmailAsync(email, subject, htmlContent, textContent);
        }

        public async Task<bool> SendEmailChangeConfirmationAsync(string email, string confirmationCode)
        {
            if (string.IsNullOrEmpty(_apiToken))
            {
                _logger.LogWarning("MailerSend API token không được cấu hình.");
                return false;
            }

            var subject = "Xác thực thay đổi email - ChatApp";
            var htmlContent = $@"
                <div style='max-width: 600px; margin: 0 auto; font-family: Arial, sans-serif;'>
                    <h2 style='color: #333; text-align: center;'>Xác thực thay đổi email - ChatApp</h2>
                    <p>Chào bạn,</p>
                    <p>Bạn đã yêu cầu thay đổi email. Để hoàn tất việc thay đổi, vui lòng sử dụng mã xác thực dưới đây:</p>
                    <div style='text-align: center; margin: 20px 0;'>
                        <span style='font-size: 32px; font-weight: bold; color: #007bff; background-color: #f8f9fa; padding: 10px 20px; border-radius: 5px; letter-spacing: 2px;'>{confirmationCode}</span>
                    </div>
                    <p>Mã này có hiệu lực trong <strong>15 phút</strong>.</p>
                    <p>Nếu bạn không yêu cầu thay đổi email này, vui lòng bỏ qua email này.</p>
                    <p>Trân trọng,<br><strong>Đội ngũ ChatApp</strong></p>
                </div>";
            var textContent = $"Mã xác thực thay đổi email: {confirmationCode}. Có hiệu lực trong 15 phút.";

            return await SendEmailAsync(email, subject, htmlContent, textContent);
        }

        public async Task<bool> SendEmailVerificationAsync(string email, string confirmationCode)
        {
            if (string.IsNullOrEmpty(_apiToken))
            {
                _logger.LogWarning("MailerSend API token không được cấu hình.");
                return false;
            }

            var subject = "Xác thực email - ChatApp";
            var htmlContent = $@"
                <div style='max-width: 600px; margin: 0 auto; font-family: Arial, sans-serif;'>
                    <h2 style='color: #333; text-align: center;'>Xác thực email - ChatApp</h2>
                    <p>Chào bạn,</p>
                    <p>Để hoàn tất việc xác thực email, vui lòng sử dụng mã xác thực dưới đây:</p>
                    <div style='text-align: center; margin: 20px 0;'>
                        <span style='font-size: 32px; font-weight: bold; color: #007bff; background-color: #f8f9fa; padding: 10px 20px; border-radius: 5px; letter-spacing: 2px;'>{confirmationCode}</span>
                    </div>
                    <p>Mã này có hiệu lực trong <strong>15 phút</strong>.</p>
                    <p>Nếu bạn không yêu cầu xác thực email này, vui lòng bỏ qua email này.</p>
                    <p>Trân trọng,<br><strong>Đội ngũ ChatApp</strong></p>
                </div>";
            
            var textContent = $"Mã xác thực email: {confirmationCode}. Có hiệu lực trong 15 phút.";

            return await SendEmailAsync(email, subject, htmlContent, textContent);
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent, string textContent)
        {
            try
            {
                var emailData = new
                {
                    from = new { email = _senderEmail },
                    to = new[] { new { email = toEmail } },
                    subject = subject,
                    text = textContent,
                    html = htmlContent
                };

                var json = JsonSerializer.Serialize(emailData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiToken}");
                _httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

                var response = await _httpClient.PostAsync("https://api.mailersend.com/v1/email", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Email gửi thành công tới {toEmail}");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Lỗi gửi email: {response.StatusCode} - {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email qua MailerSend");
                return false;
            }
        }
    }
}
