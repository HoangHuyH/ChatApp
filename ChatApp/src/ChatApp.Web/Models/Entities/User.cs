using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace ChatApp.Web.Models.Entities
{
    public class User : IdentityUser
    {
        public string DisplayName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
        public virtual ICollection<Group> CreatedGroups { get; set; } = new List<Group>();
        public virtual ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
        public virtual ICollection<Story> Stories { get; set; } = new List<Story>();
        public virtual UserStatus? Status { get; set; }
    }
}