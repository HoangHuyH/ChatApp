using System;

namespace ChatApp.Web.Models.Entities
{
    public class UserStatus
    {
        public int UserStatusId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public DateTime LastSeen { get; set; }

        // Navigation property
        public virtual User User { get; set; } = null!;
    }
}