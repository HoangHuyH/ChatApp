using System;
using System.Collections.Generic;

namespace ChatApp.Web.Models.Entities
{
    public class Group
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CreatorId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual User Creator { get; set; } = null!;
        public virtual ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}