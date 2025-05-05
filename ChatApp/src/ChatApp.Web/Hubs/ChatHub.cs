using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatApp.Web.Data;
using ChatApp.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Web.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly static Dictionary<string, string> _userConnections = new Dictionary<string, string>();
        private readonly static List<UserViewModel> _onlineUsers = new List<UserViewModel>();
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ChatHub(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task JoinRoom(string roomName)
        {
            try
            {
                var user = await _userManager.GetUserAsync(Context.User);
                if (user == null) return;

                var currentRoom = _onlineUsers
                    .FirstOrDefault(u => u.UserId == user.Id)?.CurrentRoom;

                if (currentRoom != roomName)
                {
                    // Remove user from previous room
                    if (!string.IsNullOrEmpty(currentRoom))
                    {
                        await Clients.OthersInGroup(currentRoom).SendAsync("UserLeftRoom", user.Id);
                        await Groups.RemoveFromGroupAsync(Context.ConnectionId, currentRoom);
                    }

                    // Join to new chat room
                    await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
                    
                    // Update user's current room
                    var userViewModel = _onlineUsers.FirstOrDefault(u => u.UserId == user.Id);
                    if (userViewModel != null)
                    {
                        userViewModel.CurrentRoom = roomName;
                    }

                    // Tell others to update their list of users in the room
                    await Clients.OthersInGroup(roomName).SendAsync("UserJoinedRoom", new {
                        UserId = user.Id,
                        UserName = user.UserName,
                        DisplayName = user.DisplayName,
                        CurrentRoom = roomName
                    });
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("OnError", "Failed to join room: " + ex.Message);
            }
        }

        public async Task LeaveRoom(string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
                return;
            
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);

            var user = await _userManager.GetUserAsync(Context.User);
            if (user != null)
            {
                var userViewModel = _onlineUsers.FirstOrDefault(u => u.UserId == user.Id);
                if (userViewModel != null && userViewModel.CurrentRoom == roomName)
                {
                    userViewModel.CurrentRoom = null;
                    await Clients.OthersInGroup(roomName).SendAsync("UserLeftRoom", user.Id);
                }
            }
        }

        public async Task SendPrivateMessage(string receiverId, string content)
        {
            var sender = await _userManager.GetUserAsync(Context.User);
            if (sender == null) return;

            var message = new Message
            {
                SenderId = sender.Id,
                ReceiverUserId = receiverId,
                Content = content,
                MessageType = "Text",
                SentAt = DateTime.UtcNow,
                Status = "Sent"
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Send to the sender
            await Clients.Caller.SendAsync("ReceiveMessage", new
            {
                MessageId = message.MessageId,
                Content = message.Content,
                SentAt = message.SentAt,
                Sender = new { Id = sender.Id, Name = sender.DisplayName },
                IsOwnMessage = true
            });

            // Send to the receiver if online
            if (_userConnections.TryGetValue(receiverId, out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveMessage", new
                {
                    MessageId = message.MessageId,
                    Content = message.Content,
                    SentAt = message.SentAt,
                    Sender = new { Id = sender.Id, Name = sender.DisplayName },
                    IsOwnMessage = false
                });

                // Update message status to delivered
                message.Status = "Delivered";
                await _context.SaveChangesAsync();

                // Notify the sender that the message was delivered
                await Clients.Caller.SendAsync("UpdateMessageStatus", message.MessageId, "Delivered");
            }
        }

        public async Task SendGroupMessage(int groupId, string content)
        {
            var sender = await _userManager.GetUserAsync(Context.User);
            if (sender == null) return;

            // Verify user is a member of the group
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.User.Id == sender.Id);

            if (!isMember) return;

            var message = new Message
            {
                SenderId = sender.Id,
                ReceiverGroupId = groupId,
                Content = content,
                MessageType = "Text",
                SentAt = DateTime.UtcNow,
                Status = "Sent"
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            await Clients.Group($"group_{groupId}").SendAsync("ReceiveGroupMessage", new
            {
                MessageId = message.MessageId,
                GroupId = groupId,
                Content = message.Content,
                SentAt = message.SentAt,
                Sender = new { Id = sender.Id, Name = sender.DisplayName },
                IsOwnMessage = false
            });
        }

        public async Task SendRoomMessage(string roomName, string content)
        {
            var sender = await _userManager.GetUserAsync(Context.User);
            if (sender == null) return;

            var message = new Message
            {
                SenderId = sender.Id,
                Content = content,
                MessageType = "Text",
                SentAt = DateTime.UtcNow,
                Status = "Sent"
            };

            // Find the room by name
            var room = await _context.Groups
                .FirstOrDefaultAsync(g => g.GroupName == roomName);
                
            if (room != null)
            {
                message.ReceiverGroupId = room.GroupId;
            }

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Send to others in the room (excluding the sender)
            await Clients.OthersInGroup(roomName).SendAsync("ReceiveRoomMessage", new
            {
                messageId = message.MessageId,
                roomName = roomName,
                content = message.Content,
                messageType = message.MessageType,
                sentAt = message.SentAt,
                sender = new { id = sender.Id, name = sender.DisplayName },
                isOwnMessage = false
            });
            
            // Send to the sender separately with isOwnMessage = true
            await Clients.Caller.SendAsync("ReceiveRoomMessage", new
            {
                messageId = message.MessageId,
                roomName = roomName,
                content = message.Content,
                messageType = message.MessageType,
                sentAt = message.SentAt,
                sender = new { id = sender.Id, name = sender.DisplayName },
                isOwnMessage = true
            });
        }

        public async Task MarkMessageAsRead(int messageId)
        {
            var message = await _context.Messages.FindAsync(messageId);
            if (message == null) return;

            var user = await _userManager.GetUserAsync(Context.User);
            if (user == null || (message.ReceiverUserId != user.Id && message.ReceiverGroupId == null)) return;

            message.Status = "Read";
            await _context.SaveChangesAsync();

            // Notify the sender that the message was read
            if (_userConnections.TryGetValue(message.SenderId, out var senderConnectionId))
            {
                await Clients.Client(senderConnectionId).SendAsync("UpdateMessageStatus", messageId, "Read");
            }
        }

        public async Task NotifyTyping(string receiverId)
        {
            var sender = await _userManager.GetUserAsync(Context.User);
            if (sender == null) return;

            if (_userConnections.TryGetValue(receiverId, out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveTypingIndicator", sender.Id);
            }
        }

        public async Task NotifyGroupTyping(int groupId)
        {
            var sender = await _userManager.GetUserAsync(Context.User);
            if (sender == null) return;

            // Verify user is a member of the group
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.User.Id == sender.Id);

            if (!isMember) return;

            await Clients.Group($"group_{groupId}").SendAsync("ReceiveGroupTypingIndicator", groupId, sender.Id);
        }

        public async Task NotifyRoomTyping(string roomName)
        {
            var sender = await _userManager.GetUserAsync(Context.User);
            if (sender == null) return;

            await Clients.Group(roomName).SendAsync("ReceiveRoomTypingIndicator", roomName, sender.Id);
        }

        public async Task<List<UserViewModel>> GetUsersInRoom(string roomName)
        {
            return _onlineUsers.Where(u => u.CurrentRoom == roomName).ToList();
        }

        public override async Task OnConnectedAsync()
        {
            var user = await _userManager.GetUserAsync(Context.User);
            if (user != null)
            {
                _userConnections[user.Id] = Context.ConnectionId;
                
                // Add to online users list
                if (!_onlineUsers.Any(u => u.UserId == user.Id))
                {
                    _onlineUsers.Add(new UserViewModel
                    {
                        UserId = user.Id,
                        UserName = user.UserName,
                        DisplayName = user.DisplayName,
                        IsOnline = true,
                        CurrentRoom = null,
                        Device = GetUserDevice()
                    });
                }
                
                // Update user status to online
                var userStatus = await _context.UserStatuses.FirstOrDefaultAsync(us => us.UserId == user.Id);
                if (userStatus == null)
                {
                    userStatus = new UserStatus
                    {
                        UserId = user.Id,
                        IsOnline = true,
                        LastSeen = DateTime.UtcNow
                    };
                    _context.UserStatuses.Add(userStatus);
                }
                else
                {
                    userStatus.IsOnline = true;
                    userStatus.LastSeen = DateTime.UtcNow;
                }
                
                await _context.SaveChangesAsync();
                
                // Notify friends about the user's online status
                var friends = await GetUserFriendsAsync(user.Id);
                foreach (var friend in friends)
                {
                    if (_userConnections.TryGetValue(friend.Id, out var connectionId))
                    {
                        await Clients.Client(connectionId).SendAsync("UpdateFriendStatus", user.Id, true);
                    }
                }

                // Join all groups the user is a member of
                var userGroups = await _context.GroupMembers
                    .Where(gm => gm.User.Id == user.Id)
                    .Select(gm => gm.GroupId)
                    .ToListAsync();

                foreach (var groupId in userGroups)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{groupId}");
                }
                
                // Send user information to caller
                await Clients.Caller.SendAsync("GetProfileInfo", new UserViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    DisplayName = user.DisplayName,
                    IsOnline = true,
                    CurrentRoom = null,
                    Device = GetUserDevice()
                });
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var user = await _userManager.GetUserAsync(Context.User);
            if (user != null)
            {
                // Remove from online users list
                var userViewModel = _onlineUsers.FirstOrDefault(u => u.UserId == user.Id);
                if (userViewModel != null)
                {
                    // If the user was in a room, notify others
                    if (!string.IsNullOrEmpty(userViewModel.CurrentRoom))
                    {
                        await Clients.Group(userViewModel.CurrentRoom).SendAsync("UserLeftRoom", user.Id);
                    }
                    
                    _onlineUsers.Remove(userViewModel);
                }
                
                // Remove from connections dictionary
                if (_userConnections.ContainsKey(user.Id))
                {
                    _userConnections.Remove(user.Id);
                }

                // Update user status to offline
                var userStatus = await _context.UserStatuses.FirstOrDefaultAsync(us => us.UserId == user.Id);
                if (userStatus != null)
                {
                    userStatus.IsOnline = false;
                    userStatus.LastSeen = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                // Notify friends about the user's offline status
                var friends = await GetUserFriendsAsync(user.Id);
                foreach (var friend in friends)
                {
                    if (_userConnections.TryGetValue(friend.Id, out var connectionId))
                    {
                        await Clients.Client(connectionId).SendAsync("UpdateFriendStatus", user.Id, false);
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        private async Task<List<User>> GetUserFriendsAsync(string userId)
        {
            var acceptedFriendships = await _context.Friendships
                .Where(f => (f.User1Id == userId || f.User2Id == userId) && f.Status == "Accepted")
                .ToListAsync();

            var friendIds = acceptedFriendships
                .Select(f => f.User1Id == userId ? f.User2Id : f.User1Id)
                .ToList();

            return await _userManager.Users
                .Where(u => friendIds.Contains(u.Id))
                .ToListAsync();
        }
        
        private string GetUserDevice()
        {
            var device = Context.GetHttpContext().Request.Headers["Device"].ToString();
            if (!string.IsNullOrEmpty(device) && (device.Equals("Desktop") || device.Equals("Mobile")))
                return device;

            return "Web";
        }
    }
    
    public class UserViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public bool IsOnline { get; set; }
        public string CurrentRoom { get; set; }
        public string Device { get; set; }
    }
}