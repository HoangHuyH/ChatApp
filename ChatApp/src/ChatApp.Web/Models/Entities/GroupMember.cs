using System;

namespace ChatApp.Web.Models.Entities
{
    public class GroupMember
    {
        public int GroupMemberId { get; set; }
        public int GroupId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Role { get; set; } = "Member"; // Admin, Member
        public DateTime JoinedAt { get; set; }

        // Navigation properties
        public virtual Group Group { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}