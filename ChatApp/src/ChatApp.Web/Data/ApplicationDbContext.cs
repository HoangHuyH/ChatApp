using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ChatApp.Web.Models.Entities;

namespace ChatApp.Web.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Message> Messages { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<Friendship> Friendships { get; set; }
    public DbSet<Story> Stories { get; set; }
    public DbSet<UserStatus> UserStatuses { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Message entity
        builder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId);
            entity.HasOne(e => e.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(e => e.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ReceiverUser)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(e => e.ReceiverUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ReceiverGroup)
                .WithMany(g => g.Messages)
                .HasForeignKey(e => e.ReceiverGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasCheckConstraint("CK_Messages_ReceiverCheck", 
                "ReceiverUserId IS NOT NULL OR ReceiverGroupId IS NOT NULL");

            entity.HasCheckConstraint("CK_Messages_MessageType", 
                "MessageType IN ('Text', 'Image', 'Video', 'File', 'Voice')");

            entity.HasCheckConstraint("CK_Messages_Status", 
                "Status IN ('Sent', 'Delivered', 'Read')");
        });

        // Configure Group entity
        builder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.GroupId);
            entity.HasOne(e => e.Creator)
                .WithMany(u => u.CreatedGroups)
                .HasForeignKey(e => e.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure GroupMember entity
        builder.Entity<GroupMember>(entity =>
        {
            entity.HasKey(e => e.GroupMemberId);
            entity.HasOne(e => e.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(e => e.GroupId);

            entity.HasOne(e => e.User)
                .WithMany(u => u.GroupMemberships)
                .HasForeignKey(e => e.UserId);

            entity.HasIndex(e => new { e.GroupId, e.UserId }).IsUnique();
            entity.HasCheckConstraint("CK_GroupMembers_Role", "Role IN ('Admin', 'Member')");
        });

        // Configure Friendship entity
        builder.Entity<Friendship>(entity =>
        {
            entity.HasKey(e => e.FriendshipId);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Friend)
                .WithMany()
                .HasForeignKey(e => e.FriendId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.UserId, e.FriendId }).IsUnique();
            entity.HasCheckConstraint("CK_Friendships_Status",
                "Status IN ('Pending', 'Accepted', 'Declined', 'Blocked')");
            
            // Modified to use string comparison
            // We'll handle this in the application logic instead of database constraint
            // entity.HasCheckConstraint("CK_Friendships_Users", "UserId < FriendId");
        });

        // Configure Story entity
        builder.Entity<Story>(entity =>
        {
            entity.HasKey(e => e.StoryId);
            entity.HasOne(e => e.User)
                .WithMany(u => u.Stories)
                .HasForeignKey(e => e.UserId);

            entity.HasCheckConstraint("CK_Stories_ContentType", "ContentType IN ('Text', 'Image')");
        });

        // Configure UserStatus entity
        builder.Entity<UserStatus>(entity =>
        {
            entity.HasKey(e => e.UserStatusId);
            entity.HasOne(e => e.User)
                .WithOne(u => u.Status)
                .HasForeignKey<UserStatus>(e => e.UserId);

            entity.HasIndex(e => e.UserId).IsUnique();
        });

        // Seed some initial data
        SeedData(builder);
    }

    private void SeedData(ModelBuilder builder)
    {
        // We'll add seed data after the initial migration
    }
}
