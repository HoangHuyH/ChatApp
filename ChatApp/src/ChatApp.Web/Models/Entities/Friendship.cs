using System;

namespace ChatApp.Web.Models.Entities
{
    public class Friendship
    {
        public int FriendshipId { get; set; }
        public string User1Id { get; set; } = string.Empty;
        public string User2Id { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Declined, Blocked
        public DateTime RequestedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }

        // Navigation properties
        public virtual User User1 { get; set; } = null!;
        public virtual User User2 { get; set; } = null!;
    }
}