using System;

namespace ChatApp.Web.Models.ViewModels
{
    public class UserViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public string? CurrentRoom { get; set; }
        public string Device { get; set; } = "Web";
        public bool IsOnline { get; set; } = true;
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    }
}
