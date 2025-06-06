using ChatApp.Web.Data;
using ChatApp.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApp.Web.Services
{
    public class PasswordResetService : IPasswordResetService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;

        public PasswordResetService(
            ApplicationDbContext context,
            UserManager<User> userManager,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        public async Task<bool> SendPasswordResetCodeAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return false;

            // Vô hiệu hóa các token cũ
            var oldTokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id && !t.IsUsed)
                .ToListAsync();

            foreach (var token in oldTokens)
            {
                token.IsUsed = true;
            }

            // Tạo mã 6 số ngẫu nhiên
            var resetCode = GenerateResetCode();
            
            // Tạo token mới
            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = resetCode,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            // Gửi email
            return await _emailService.SendPasswordResetEmailAsync(email, resetCode);
        }

        public async Task<bool> ValidateResetCodeAsync(string email, string code)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return false;

            var token = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.UserId == user.Id && 
                                         t.Token == code && 
                                         !t.IsUsed && 
                                         t.ExpiresAt > DateTime.UtcNow);

            return token != null;
        }

        public async Task<bool> ResetPasswordAsync(string email, string code, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return false;

            var token = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.UserId == user.Id && 
                                         t.Token == code && 
                                         !t.IsUsed && 
                                         t.ExpiresAt > DateTime.UtcNow);

            if (token == null)
                return false;

            // Đặt lại mật khẩu
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (result.Succeeded)
            {
                // Đánh dấu token đã sử dụng
                token.IsUsed = true;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task CleanupExpiredTokensAsync()
        {
            var expiredTokens = await _context.PasswordResetTokens
                .Where(t => t.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            _context.PasswordResetTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();
        }

        private string GenerateResetCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}
