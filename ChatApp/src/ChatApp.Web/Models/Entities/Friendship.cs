using System;

namespace ChatApp.Web.Models.Entities
{
    public class Friendship
    {
        public int FriendshipId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FriendId { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Declined, Blocked
        public DateTime RequestedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual User Friend { get; set; } = null!;
    }
}