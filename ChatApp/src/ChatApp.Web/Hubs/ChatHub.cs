using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatApp.Web.Data;
using ChatApp.Web.Models.Entities;
using ChatApp.Web.Models.ViewModels;
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

        // Static method to access online users from outside the hub
        public static List<UserViewModel> GetOnlineUsers()
        {
            // Return a copy of the online users to prevent external modification
            return _onlineUsers.ToList();
        }

        public async Task JoinRoom(string roomName)
        {
            try
            {
                var user = await _userManager.GetUserAsync(Context.User);
                if (user == null) return;

                // Join to chat room regardless of current room
                await Groups.AddToGroupAsync(Context.ConnectionId, roomName);

                // Update user's CurrentRoom in UserStatus table
                var userStatus = await _context.UserStatuses.FirstOrDefaultAsync(us => us.UserId == user.Id);
                if (userStatus != null)
                {
                    var previousRoom = userStatus.CurrentRoom;
                    userStatus.CurrentRoom = roomName;
                    await _context.SaveChangesAsync();
                    
                    // If coming from another room, leave that room's SignalR group
                    if (!string.IsNullOrEmpty(previousRoom) && previousRoom != roomName)
                    {
                        await Groups.RemoveFromGroupAsync(Context.ConnectionId, previousRoom);
                        await Clients.OthersInGroup(previousRoom).SendAsync("UserLeftRoom", user.Id);
                    }
                }
                
                // Find and update the user's entry in online users list
                var userViewModel = _onlineUsers.FirstOrDefault(u => u.UserId == user.Id);
                if (userViewModel != null)
                {
                    var currentRoom = userViewModel.CurrentRoom;

                    // Only notify about leaving previous room if it's different
                    if (!string.IsNullOrEmpty(currentRoom) && currentRoom != roomName)
                    {
                        await Clients.OthersInGroup(currentRoom).SendAsync("UserLeftRoom", user.Id);
                        await Groups.RemoveFromGroupAsync(Context.ConnectionId, currentRoom);
                    }

                    // Update user's current room
                    userViewModel.CurrentRoom = roomName;

                    // Notify others in the room about the user joining
                    // Only send if not already in this room (prevents duplicate notifications)
                    if (currentRoom != roomName)
                    {
                        await Clients.OthersInGroup(roomName).SendAsync("UserJoinedRoom", new {
                            userId = user.Id,
                            userName = user.UserName,
                            displayName = user.DisplayName,
                            currentRoom = roomName,
                            isOnline = true,
                            device = userViewModel.Device
                        });
                    }
                }
                else
                {
                    // If user not found in online users list (should be rare), add them
                    userViewModel = new UserViewModel
                    {
                        UserId = user.Id,
                        UserName = user.UserName,
                        DisplayName = user.DisplayName,
                        IsOnline = true,
                        CurrentRoom = roomName,
                        Device = GetUserDevice()
                    };
                    _onlineUsers.Add(userViewModel);
                    
                    // Notify others about new user
                    await Clients.OthersInGroup(roomName).SendAsync("UserJoinedRoom", new {
                        userId = user.Id,
                        userName = user.UserName,
                        displayName = user.DisplayName,
                        currentRoom = roomName,
                        isOnline = true,
                        device = userViewModel.Device
                    });
                }
                    
                // Check if user is already a member in the database
                var room = await _context.Groups.FirstOrDefaultAsync(g => g.GroupName == roomName);
                if (room != null)
                {
                    var isMember = await _context.GroupMembers
                        .AnyAsync(gm => gm.GroupId == room.GroupId && gm.UserId == user.Id);
                        
                    // If not a member yet, add them as a permanent member
                    if (!isMember)
                    {
                        var membership = new GroupMember
                        {
                            GroupId = room.GroupId,
                            UserId = user.Id,
                            Role = "Member",
                            JoinedAt = DateTime.UtcNow
                        };
                        _context.GroupMembers.Add(membership);
                        await _context.SaveChangesAsync();
                        
                        // Notify everyone that a new user has been added permanently to the room
                        await Clients.Group($"group_{room.GroupId}").SendAsync("UserAddedToRoom", new {
                            roomId = room.GroupId,
                            roomName = roomName,
                            userId = user.Id,
                            displayName = user.DisplayName
                        });
                    }
                }
                  
                // Use GetUsersInRoom which handles deduplication properly
                var roomMembers = await GetUsersInRoom(roomName);
                
                // Filter out the current user from the list
                var filteredRoomMembers = roomMembers
                    .Where(u => u.UserId != user.Id)
                    .ToList();
                
                await Clients.Caller.SendAsync("UpdateRoomUsers", filteredRoomMembers);
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
                
                // Update user status in database to reflect the room change
                var userStatus = await _context.UserStatuses.FirstOrDefaultAsync(us => us.UserId == user.Id);
                if (userStatus != null && userStatus.CurrentRoom == roomName)
                {
                    userStatus.CurrentRoom = null;
                    await _context.SaveChangesAsync();
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
                MessageId = message.MessageId,
                RoomName = roomName,
                Content = message.Content,
                MessageType = message.MessageType,
                SentAt = message.SentAt,
                Sender = new { Id = sender.Id, Name = sender.DisplayName },
                IsOwnMessage = false
            });
            
            // Send to the sender separately with isOwnMessage = true
            await Clients.Caller.SendAsync("ReceiveRoomMessage", new
            {
                MessageId = message.MessageId,
                RoomName = roomName,
                Content = message.Content,
                MessageType = message.MessageType,
                SentAt = message.SentAt,
                Sender = new { Id = sender.Id, Name = sender.DisplayName },
                IsOwnMessage = true
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
            // Find the room in the database
            var room = await _context.Groups.FirstOrDefaultAsync(g => g.GroupName == roomName);
            
            // Get online users who are in this room (without duplicates)
            var onlineRoomUsers = _onlineUsers
                .Where(u => u.CurrentRoom == roomName)
                .GroupBy(u => u.UserId)  // Group by user ID to remove duplicates
                .Select(g => g.First())   // Take the first occurrence from each group
                .ToList();
                
            if (room == null) 
            {
                // If no room in database, just return the deduplicated online users in this room
                return onlineRoomUsers;
            }
            
            // Get all member users from the database
            var roomMembers = await _context.GroupMembers
                .Where(gm => gm.GroupId == room.GroupId)
                .Select(gm => new UserViewModel
                {
                    UserId = gm.UserId,
                    DisplayName = gm.User.DisplayName,
                    UserName = gm.User.UserName,
                    IsOnline = gm.User.Status != null && gm.User.Status.IsOnline,
                    Device = "Web"
                })
                .ToListAsync();
                
            // Create a dictionary for fast lookup of database users
            var memberDict = roomMembers.ToDictionary(m => m.UserId);
                
            // Update database users with online status and merge online-only users
            foreach (var onlineUser in onlineRoomUsers)
            {
                // If user exists in database members, update with online information
                if (memberDict.TryGetValue(onlineUser.UserId, out var existingMember))
                {
                    existingMember.IsOnline = true;
                    existingMember.Device = onlineUser.Device;
                    existingMember.CurrentRoom = onlineUser.CurrentRoom;
                }
                else
                {
                    // If user is online in the room but not in database members, add them
                    roomMembers.Add(onlineUser);
                }
            }
            
            return roomMembers;
        }

        public async Task InviteUserToGroup(int groupId, string userId)
        {
            var sender = await _userManager.GetUserAsync(Context.User);
            if (sender == null) return;

            // Check if sender is admin
            var senderMembership = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == sender.Id && gm.Role == "Admin");
            
            if (senderMembership == null)
            {
                await Clients.Caller.SendAsync("OnError", "Only administrators can invite users to groups");
                return;
            }

            // Send notification to the invited user
            await Clients.User(userId).SendAsync("GroupInviteReceived", new
            {
                groupId = groupId,
                groupName = (await _context.Groups.FindAsync(groupId))?.GroupName,
                inviterId = sender.Id,
                inviterName = sender.DisplayName
            });
        }

        public async Task AcceptGroupInvite(int groupId)
        {
            var user = await _userManager.GetUserAsync(Context.User);
            if (user == null) return;

            var group = await _context.Groups.FindAsync(groupId);
            if (group == null) return;
            
            // Check if already a member
            var existingMembership = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == user.Id);
            
            if (existingMembership) return;
            
            // Add user to group
            var membership = new GroupMember
            {
                GroupId = groupId,
                UserId = user.Id,
                Role = "Member",
                JoinedAt = DateTime.UtcNow
            };
            
            _context.GroupMembers.Add(membership);
            await _context.SaveChangesAsync();
            
            // Add to SignalR group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{groupId}");
            
            // Notify group members
            await Clients.Group($"group_{groupId}").SendAsync("UserJoinedGroup", new
            {
                groupId = groupId,
                userId = user.Id,
                displayName = user.DisplayName,
                role = "Member",
                isOnline = true
            });
            
            // Signal client to reload groups list
            await Clients.Caller.SendAsync("ReloadGroups");
        }

        public async Task GetGroupMembers(int groupId)
        {
            var user = await _userManager.GetUserAsync(Context.User);
            if (user == null) return;
            
            // Check if user is a member of the group
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == user.Id);
            
            if (!isMember)
            {
                await Clients.Caller.SendAsync("OnError", "You must be a member of the group to view its members");
                return;
            }
            
            var members = await _context.GroupMembers
                .Where(gm => gm.GroupId == groupId)
                .Select(gm => new
                {
                    userId = gm.UserId,
                    displayName = gm.User.DisplayName,
                    userName = gm.User.UserName,
                    role = gm.Role,
                    joinedAt = gm.JoinedAt,
                    isOnline = gm.User.Status != null && gm.User.Status.IsOnline,
                    lastSeen = gm.User.Status != null ? gm.User.Status.LastSeen : (DateTime?)null
                })
                .ToListAsync();
            
            await Clients.Caller.SendAsync("ReceiveGroupMembers", members);
        }

        public override async Task OnConnectedAsync()
        {
            if (Context.User != null)
            {
                var user = await _userManager.GetUserAsync(Context.User);
                if (user != null)
                {
                    // Store connection ID
                    if (_userConnections.ContainsKey(user.Id))
                    {
                        _userConnections[user.Id] = Context.ConnectionId;
                    }
                    else
                    {
                        _userConnections.Add(user.Id, Context.ConnectionId);
                    }

                    // Remove any existing entries for this user before adding a new one
                    _onlineUsers.RemoveAll(u => u.UserId == user.Id);
                      // Get current room if user was in one (from database or other connection)
                    string currentRoom = null;
                    var userStatus = await _context.UserStatuses.FirstOrDefaultAsync(us => us.UserId == user.Id);                    if (userStatus != null && !string.IsNullOrEmpty(userStatus.CurrentRoom))
                    {
                        currentRoom = userStatus.CurrentRoom;
                    }                    // Add user to online users list
                    _onlineUsers.Add(new UserViewModel
                    {
                        UserId = user.Id,
                        UserName = user.UserName,
                        DisplayName = user.UserName, // You might want to get actual DisplayName if available
                        ConnectionId = Context.ConnectionId,
                        CurrentRoom = currentRoom
                    });

                    // Join the user to their last room if they had one
                    if (!string.IsNullOrEmpty(currentRoom))
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, currentRoom);
                        await Clients.OthersInGroup(currentRoom).SendAsync("UserJoinedRoom", user.Id);
                    }
                    
                    // Notify all clients of user status change
                    await Clients.All.SendAsync("UpdateUserStatus", user.Id, true);
                }
            }
            
            await base.OnConnectedAsync();
        }
        private string GetUserDevice()
        {
            // In a real implementation, this would parse the User-Agent string
            // For simplicity, we'll just return "Web" as default
            return "Web";
        }
        // Helper method to determine the device type from user agent string        private string GetUserDevice()
        // {
        //     // In a real implementation, this would parse the User-Agent string
        //     // For simplicity, we'll just return "Web" as default
        //     return "Web";
        // }
        
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Context.User != null)
            {
                var user = await _userManager.GetUserAsync(Context.User);
                if (user != null)
                {
                    // Check if this is the user's only connection
                    bool userHasOtherConnections = false;
                    if (_userConnections.TryGetValue(user.Id, out var connectionId) && connectionId != Context.ConnectionId)
                    {
                        userHasOtherConnections = true;
                    }
                    
                    if (!userHasOtherConnections)
                    {
                        // Remove from dictionaries
                        _userConnections.Remove(user.Id);
                        _onlineUsers.RemoveAll(u => u.UserId == user.Id);
                        
                        // Update user status in database
                        var userStatus = await _context.UserStatuses.FirstOrDefaultAsync(us => us.UserId == user.Id);
                        if (userStatus != null)
                        {
                            userStatus.IsOnline = false;
                            userStatus.LastSeen = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                        }
                        
                        // Notify all clients of user status change
                        await Clients.All.SendAsync("UpdateUserStatus", user.Id, false);
                    }
                }
            }
            
            await base.OnDisconnectedAsync(exception);
        }
    }
}