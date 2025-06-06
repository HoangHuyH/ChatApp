using Microsoft.Extensions.Configuration;
using RestSharp;
using RestSharp.Authenticators;
using System.Threading.Tasks;

namespace ChatApp.Web.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _domain;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _apiKey = _configuration["MAILGUN_API_KEY"] ?? "";
            _domain = _configuration["MAILGUN_DOMAIN"] ?? "";
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string resetCode)
        {
            try
            {
                var subject = "Đặt lại mật khẩu - ChatApp";
                var htmlMessage = $@"
                    <h2>Đặt lại mật khẩu - ChatApp</h2>
                    <p>Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản ChatApp.</p>
                    <p><strong>Mã xác thực: {resetCode}</strong></p>
                    <p>Mã này có hiệu lực trong 15 phút.</p>
                    <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
                ";

                return await SendEmailAsync(email, subject, htmlMessage);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SendEmailChangeConfirmationAsync(string email, string confirmationCode)
        {
            try
            {
                var subject = "Xác thực thay đổi email - ChatApp";
                var htmlMessage = $@"
                    <h2>Xác thực thay đổi email - ChatApp</h2>
                    <p>Bạn đã yêu cầu thay đổi email cho tài khoản ChatApp.</p>
                    <p><strong>Mã xác thực: {confirmationCode}</strong></p>
                    <p>Mã này có hiệu lực trong 15 phút.</p>
                    <p>Nếu bạn không yêu cầu thay đổi email, vui lòng bỏ qua email này.</p>
                ";

                return await SendEmailAsync(email, subject, htmlMessage);
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_domain))
                {
                    Console.WriteLine("Mailgun API key hoặc domain không được cấu hình.");
                    return false;
                }

                var options = new RestClientOptions("https://api.mailgun.net")
                {
                    Authenticator = new HttpBasicAuthenticator("api", _apiKey)
                };

                var client = new RestClient(options);
                var request = new RestRequest($"/v3/{_domain}/messages", Method.Post);
                request.AlwaysMultipartFormData = true;
                request.AddParameter("from", $"ChatApp <postmaster@{_domain}>");
                request.AddParameter("to", toEmail);
                request.AddParameter("subject", subject);
                request.AddParameter("html", htmlMessage);
                request.AddParameter("text", htmlMessage.Replace("<h2>", "").Replace("</h2>", "\n").Replace("<p>", "").Replace("</p>", "\n").Replace("<strong>", "").Replace("</strong>", ""));

                var response = await client.ExecuteAsync(request);
                
                if (response.IsSuccessful)
                {
                    Console.WriteLine($"Email gửi thành công tới {toEmail}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Lỗi gửi email: {response.StatusCode} - {response.Content}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi gửi email: {ex.Message}");
                return false;
            }
        }
    }
}
