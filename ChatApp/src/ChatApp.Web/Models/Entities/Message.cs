using System;

namespace ChatApp.Web.Models.Entities
{
    public class Message
    {
        public int MessageId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string? ReceiverUserId { get; set; }
        public int? ReceiverGroupId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string MessageType { get; set; } = "Text"; // Text, Image, Video, File, Voice
        public string? FilePath { get; set; }
        public DateTime SentAt { get; set; }
        public string Status { get; set; } = "Sent"; // Sent, Delivered, Read

        // Navigation properties
        public virtual User Sender { get; set; } = null!;
        public virtual User? ReceiverUser { get; set; }
        public virtual Group? ReceiverGroup { get; set; }
    }
}