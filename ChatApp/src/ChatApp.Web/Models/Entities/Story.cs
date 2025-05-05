using System;

namespace ChatApp.Web.Models.Entities
{
    public class Story
    {
        public int StoryId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string ContentType { get; set; } = "Text"; // Text, Image
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }
}