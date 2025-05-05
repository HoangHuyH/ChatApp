using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ChatApp.Web.Data;
using ChatApp.Web.Models.Entities;

namespace ChatApp.Web.Services
{
    public class DatabaseInitializer
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(
            ApplicationDbContext context,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<DatabaseInitializer> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            // Apply migrations
            await _context.Database.MigrateAsync();

            // Create test users
            if (!_userManager.Users.Any())
            {
                _logger.LogInformation("Seeding test users");
                
                // Create test users
                var users = new[]
                {
                    new User 
                    { 
                        UserName = "john@example.com", 
                        Email = "john@example.com",
                        DisplayName = "John Doe",
                        CreatedAt = DateTime.UtcNow
                    },
                    new User 
                    { 
                        UserName = "jane@example.com", 
                        Email = "jane@example.com",
                        DisplayName = "Jane Smith",
                        CreatedAt = DateTime.UtcNow
                    },
                    new User 
                    { 
                        UserName = "bob@example.com", 
                        Email = "bob@example.com",
                        DisplayName = "Bob Johnson",
                        CreatedAt = DateTime.UtcNow
                    }
                };

                string defaultPassword = "Password123!";

                foreach (var user in users)
                {
                    await _userManager.CreateAsync(user, defaultPassword);
                }

                // Create friendships between users
                var john = await _userManager.FindByNameAsync("john@example.com");
                var jane = await _userManager.FindByNameAsync("jane@example.com");
                var bob = await _userManager.FindByNameAsync("bob@example.com");

                if (john != null && jane != null && bob != null)
                {
                    // John and Jane are friends
                    _context.Friendships.Add(new Friendship
                    {
                        User1Id = john.Id,
                        User2Id = jane.Id,
                        Status = "Accepted",
                        RequestedAt = DateTime.UtcNow.AddDays(-5),
                        AcceptedAt = DateTime.UtcNow.AddDays(-4)
                    });

                    // John and Bob are friends
                    _context.Friendships.Add(new Friendship
                    {
                        User1Id = john.Id,
                        User2Id = bob.Id,
                        Status = "Accepted",
                        RequestedAt = DateTime.UtcNow.AddDays(-3),
                        AcceptedAt = DateTime.UtcNow.AddDays(-2)
                    });

                    // Jane sent friend request to Bob
                    _context.Friendships.Add(new Friendship
                    {
                        User1Id = jane.Id,
                        User2Id = bob.Id,
                        Status = "Pending",
                        RequestedAt = DateTime.UtcNow.AddHours(-5)
                    });

                    // Create a group chat
                    var group = new Group
                    {
                        GroupName = "Friends Group",
                        CreatorId = john.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    };
                    _context.Groups.Add(group);
                    await _context.SaveChangesAsync();

                    // Add members to the group
                    _context.GroupMembers.Add(new GroupMember
                    {
                        GroupId = group.GroupId,
                        UserId = john.Id,
                        Role = "Admin",
                        JoinedAt = DateTime.UtcNow.AddDays(-1)
                    });

                    _context.GroupMembers.Add(new GroupMember
                    {
                        GroupId = group.GroupId,
                        UserId = jane.Id,
                        Role = "Member",
                        JoinedAt = DateTime.UtcNow.AddDays(-1)
                    });

                    // Add sample messages
                    _context.Messages.Add(new Message
                    {
                        SenderId = john.Id,
                        ReceiverUserId = jane.Id,
                        Content = "Hi Jane, how are you?",
                        MessageType = "Text",
                        SentAt = DateTime.UtcNow.AddHours(-3),
                        Status = "Read"
                    });

                    _context.Messages.Add(new Message
                    {
                        SenderId = jane.Id,
                        ReceiverUserId = john.Id,
                        Content = "Hey John! I'm good, thanks for asking.",
                        MessageType = "Text",
                        SentAt = DateTime.UtcNow.AddHours(-2.5),
                        Status = "Read"
                    });

                    _context.Messages.Add(new Message
                    {
                        SenderId = john.Id,
                        ReceiverUserId = bob.Id,
                        Content = "Hey Bob, what's up?",
                        MessageType = "Text",
                        SentAt = DateTime.UtcNow.AddHours(-1),
                        Status = "Delivered"
                    });

                    _context.Messages.Add(new Message
                    {
                        SenderId = john.Id,
                        ReceiverGroupId = group.GroupId,
                        Content = "Welcome to our group chat!",
                        MessageType = "Text",
                        SentAt = DateTime.UtcNow.AddHours(-0.5),
                        Status = "Sent"
                    });

                    // Add user statuses
                    _context.UserStatuses.Add(new UserStatus
                    {
                        UserId = john.Id,
                        IsOnline = false,
                        LastSeen = DateTime.UtcNow.AddMinutes(-30)
                    });

                    _context.UserStatuses.Add(new UserStatus
                    {
                        UserId = jane.Id,
                        IsOnline = false,
                        LastSeen = DateTime.UtcNow.AddHours(-1)
                    });

                    _context.UserStatuses.Add(new UserStatus
                    {
                        UserId = bob.Id,
                        IsOnline = false,
                        LastSeen = DateTime.UtcNow.AddHours(-2)
                    });

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Test data seeded successfully");
                }
            }
        }
    }
}