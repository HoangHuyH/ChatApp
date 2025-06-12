using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using ChatApp.Web.Data;
using ChatApp.Web.Models.Entities;
using Microsoft.AspNetCore.SignalR;
using ChatApp.Web.Hubs;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;

namespace ChatApp.Web.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IWebHostEnvironment _environment;

        public ChatController(
            ApplicationDbContext context, 
            UserManager<User> userManager,
            IHubContext<ChatHub> hubContext,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> GetChatHistory(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID is required");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var messages = await _context.Messages
                .Where(m => 
                    (m.SenderId == currentUser.Id && m.ReceiverUserId == userId) ||
                    (m.SenderId == userId && m.ReceiverUserId == currentUser.Id))
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    MessageId = m.MessageId,
                    Content = m.Content,
                    MessageType = m.MessageType,
                    SentAt = m.SentAt,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.DisplayName,
                    Status = m.Status,
                    IsOwnMessage = m.SenderId == currentUser.Id
                })
                .ToListAsync();

            // Mark messages as read
            var unreadMessages = await _context.Messages
                .Where(m => m.SenderId == userId && m.ReceiverUserId == currentUser.Id && m.Status != "Read")
                .ToListAsync();

            if (unreadMessages.Any())
            {
                foreach (var message in unreadMessages)
                {
                    message.Status = "Read";
                }
                await _context.SaveChangesAsync();
            }

            return Json(messages);
        }

        [HttpGet]
        public async Task<IActionResult> GetGroupChatHistory(int groupId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // Check if user is member of the group
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.User.Id == currentUser.Id);

            if (!isMember)
            {
                return Forbid();
            }

            var messages = await _context.Messages
                .Where(m => m.ReceiverGroupId == groupId)
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    MessageId = m.MessageId,
                    GroupId = m.ReceiverGroupId,
                    Content = m.Content,
                    MessageType = m.MessageType,
                    SentAt = m.SentAt,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.DisplayName,
                    IsOwnMessage = m.SenderId == currentUser.Id
                })
                .ToListAsync();

            return Json(messages);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRooms()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // Get rooms the user is a member of in the database
            var memberRooms = await _context.GroupMembers
                .Where(gm => gm.User.Id == currentUser.Id)
                .Select(gm => new {
                    Id = gm.Group.GroupId,
                    Name = gm.Group.GroupName,
                    CreatedBy = gm.Group.CreatorId,
                    MemberCount = gm.Group.Members.Count
                })
                .ToListAsync();
            
            // Get additional rooms from SignalR hub that the user has joined but isn't a database member of
            // First, get the SignalR hub context to access online users
            var hubContext = (IHubContext<ChatHub>)HttpContext.RequestServices.GetService(typeof(IHubContext<ChatHub>));
            
            // Access the static collection of online users from the hub
            var onlineUsers = ChatHub.GetOnlineUsers();
            var currentUserViewModel = onlineUsers.FirstOrDefault(u => u.UserId == currentUser.Id);
            
            // Add the current room the user is in if it's not already in memberRooms
            if (currentUserViewModel != null && !string.IsNullOrEmpty(currentUserViewModel.CurrentRoom))
            {
                var currentRoom = await _context.Groups
                    .FirstOrDefaultAsync(g => g.GroupName == currentUserViewModel.CurrentRoom);
                
                if (currentRoom != null && !memberRooms.Any(r => r.Id == currentRoom.GroupId))
                {
                    memberRooms.Add(new {
                        Id = currentRoom.GroupId,
                        Name = currentRoom.GroupName,
                        CreatedBy = currentRoom.CreatorId,
                        MemberCount = currentRoom.Members.Count
                    });
                }
            }

            return Json(memberRooms);
        }

        [HttpGet]
        public async Task<IActionResult> GetRoomMessages(string roomName, int take = 20)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                return BadRequest("Room name is required");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // First, find the room by its name
            var room = await _context.Groups
                .FirstOrDefaultAsync(g => g.GroupName == roomName);

            if (room == null)
            {
                return NotFound("Room not found");
            }

            // Get messages for this specific room
            var messages = await _context.Messages
                .Where(m => m.ReceiverGroupId == room.GroupId)
                .OrderByDescending(m => m.SentAt)
                .Take(take)
                .Select(m => new
                {
                    MessageId = m.MessageId,
                    Content = m.Content,
                    MessageType = m.MessageType,
                    SentAt = m.SentAt,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.DisplayName,
                    IsOwnMessage = m.SenderId == currentUser.Id
                })
                .ToListAsync();

            return Json(messages.OrderBy(m => m.SentAt).ToList());
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageViewModel model)
        {
            if (string.IsNullOrEmpty(model.Content))
            {
                return BadRequest("Message content is required");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var message = new Message
            {
                SenderId = currentUser.Id,
                Content = model.Content,
                MessageType = "Text",
                SentAt = DateTime.UtcNow,
                Status = "Sent"
            };

            // Determine if it's a private message, group message or room message
            if (!string.IsNullOrEmpty(model.ReceiverUserId))
            {
                message.ReceiverUserId = model.ReceiverUserId;

                // Add to database
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Use SignalR to broadcast the message
                await _hubContext.Clients.User(model.ReceiverUserId).SendAsync("ReceiveMessage", new
                {
                    MessageId = message.MessageId,
                    Content = message.Content,
                    MessageType = message.MessageType,
                    SentAt = message.SentAt,
                    Sender = new { Id = currentUser.Id, Name = currentUser.DisplayName },
                    IsOwnMessage = false
                });

                // Also send to the sender
                await _hubContext.Clients.User(currentUser.Id).SendAsync("ReceiveMessage", new
                {
                    MessageId = message.MessageId,
                    Content = message.Content,
                    MessageType = message.MessageType,
                    SentAt = message.SentAt,
                    Sender = new { Id = currentUser.Id, Name = currentUser.DisplayName },
                    IsOwnMessage = true
                });
            }
            else if (model.ReceiverGroupId.HasValue)
            {
                // Check if user is member of the group
                var isMember = await _context.GroupMembers
                    .AnyAsync(gm => gm.GroupId == model.ReceiverGroupId.Value && gm.User.Id == currentUser.Id);

                if (!isMember)
                {
                    return Forbid();
                }

                message.ReceiverGroupId = model.ReceiverGroupId;

                // Add to database
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Use SignalR to broadcast the message to the group
                await _hubContext.Clients.Group($"group_{model.ReceiverGroupId}").SendAsync("ReceiveGroupMessage", new
                {
                    MessageId = message.MessageId,
                    GroupId = message.ReceiverGroupId,
                    Content = message.Content,
                    MessageType = message.MessageType,
                    SentAt = message.SentAt,
                    Sender = new { Id = currentUser.Id, Name = currentUser.DisplayName },
                    IsOwnMessage = false
                });
            }
            else if (!string.IsNullOrEmpty(model.RoomName))
            {
                // Find the room by name
                var room = await _context.Groups
                    .FirstOrDefaultAsync(g => g.GroupName == model.RoomName);
                
                if (room == null)
                {
                    return NotFound("Room not found");
                }
                
                // Set the receiver group ID to the room's group ID
                message.ReceiverGroupId = room.GroupId;
                
                // Add to database
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Use SignalR to broadcast the message to the room
                await _hubContext.Clients.Group(model.RoomName).SendAsync("ReceiveRoomMessage", new
                {
                    MessageId = message.MessageId,
                    RoomName = model.RoomName,
                    Content = message.Content,
                    MessageType = message.MessageType,
                    SentAt = message.SentAt,
                    Sender = new { Id = currentUser.Id, Name = currentUser.DisplayName },
                    IsOwnMessage = false
                });
            }

            return Ok(new { MessageId = message.MessageId });
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var file = Request.Form.Files[0];
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file was uploaded.");
                }

                // Determine what kind of file it is
                string messageType = "File";
                if (file.ContentType.StartsWith("image/"))
                {
                    messageType = "Image";
                }

                // Save the file
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                var fileType = messageType == "Image" ? "images" : "files";
                var targetFolder = Path.Combine(uploadsFolder, fileType);

                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(targetFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Create a relative URL for the file
                var fileUrl = $"/uploads/{fileType}/{uniqueFileName}";

                // Get message details
                string receiverUserId = Request.Form["receiverUserId"];
                int? receiverGroupId = !string.IsNullOrEmpty(Request.Form["receiverGroupId"]) 
                    ? int.Parse(Request.Form["receiverGroupId"]) 
                    : (int?)null;
                string roomName = Request.Form["roomName"];

                // Create message content based on message type
                string content;
                if (messageType == "Image")
                {
                    content = $"<img src=\"{fileUrl}\" class=\"chat-image\" alt=\"Image\" />";
                }
                else
                {
                    content = $"<a href=\"{fileUrl}\" target=\"_blank\" class=\"chat-file\">{file.FileName}</a>";
                }

                // Create the message
                var message = new Message
                {
                    SenderId = currentUser.Id,
                    Content = content,
                    MessageType = messageType,
                    SentAt = DateTime.UtcNow,
                    Status = "Sent"
                };

                // Set the receiver based on the type of message
                if (!string.IsNullOrEmpty(receiverUserId))
                {
                    message.ReceiverUserId = receiverUserId;
                }
                else if (receiverGroupId.HasValue)
                {
                    message.ReceiverGroupId = receiverGroupId;
                }
                else if (!string.IsNullOrEmpty(roomName))
                {
                    // Find the room by name
                    var room = await _context.Groups
                        .FirstOrDefaultAsync(g => g.GroupName == roomName);
                    
                    if (room != null)
                    {
                        message.ReceiverGroupId = room.GroupId;
                    }
                }

                // Save to database
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Broadcast using SignalR based on the type of message
                if (!string.IsNullOrEmpty(receiverUserId))
                {
                    await _hubContext.Clients.User(receiverUserId).SendAsync("ReceiveMessage", new
                    {
                        MessageId = message.MessageId,
                        Content = message.Content,
                        MessageType = message.MessageType,
                        SentAt = message.SentAt,
                        Sender = new { Id = currentUser.Id, Name = currentUser.DisplayName },
                        IsOwnMessage = false
                    });

                    await _hubContext.Clients.User(currentUser.Id).SendAsync("ReceiveMessage", new
                    {
                        MessageId = message.MessageId,
                        Content = message.Content,
                        MessageType = message.MessageType,
                        SentAt = message.SentAt,
                        Sender = new { Id = currentUser.Id, Name = currentUser.DisplayName },
                        IsOwnMessage = true
                    });
                }
                else if (receiverGroupId.HasValue)
                {
                    await _hubContext.Clients.Group($"group_{receiverGroupId}").SendAsync("ReceiveGroupMessage", new
                    {
                        MessageId = message.MessageId,
                        GroupId = message.ReceiverGroupId,
                        Content = message.Content,
                        MessageType = message.MessageType,
                        SentAt = message.SentAt,
                        Sender = new { Id = currentUser.Id, Name = currentUser.DisplayName },
                        IsOwnMessage = false
                    });
                }
                else if (!string.IsNullOrEmpty(roomName))
                {
                    await _hubContext.Clients.Group(roomName).SendAsync("ReceiveRoomMessage", new
                    {
                        MessageId = message.MessageId,
                        RoomName = roomName,
                        Content = message.Content,
                        MessageType = message.MessageType,
                        SentAt = message.SentAt,
                        Sender = new { Id = currentUser.Id, Name = currentUser.DisplayName },
                        IsOwnMessage = false
                    });
                }

                return Ok(new { 
                    MessageId = message.MessageId,
                    FileUrl = fileUrl,
                    FileName = file.FileName,
                    FileType = messageType
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserInfo(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID is required");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Get user status
            var userStatus = await _context.UserStatuses
                .FirstOrDefaultAsync(us => us.UserId == userId);

            return Json(new
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                IsOnline = userStatus?.IsOnline ?? false,
                LastSeen = userStatus?.LastSeen
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomViewModel model)
        {
            if (string.IsNullOrEmpty(model.Name))
            {
                return BadRequest("Room name is required");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // Check if a group with this name already exists
            var existingGroup = await _context.Groups.FirstOrDefaultAsync(g => g.GroupName == model.Name);
            if (existingGroup != null)
            {
                return BadRequest("A room with this name already exists");
            }

            // Create new group/room
            var group = new Group
            {
                GroupName = model.Name,
                CreatorId = currentUser.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Groups.Add(group);
            
            // Add current user as member
            var membership = new GroupMember
            {
                Group = group,
                User = currentUser,
                Role = "Admin",
                JoinedAt = DateTime.UtcNow
            };
            
            _context.GroupMembers.Add(membership);
            await _context.SaveChangesAsync();

            // Notify all clients about the new room
            await _hubContext.Clients.All.SendAsync("NewRoomCreated", new
            {
                Id = group.GroupId,
                Name = group.GroupName,
                CreatedBy = group.CreatorId,
                MemberCount = 1
            });

            return Ok(new { RoomId = group.GroupId });
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupViewModel model)
        {
            if (string.IsNullOrEmpty(model.Name))
            {
                return BadRequest("Group name is required");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // Check if a group with this name already exists
            var existingGroup = await _context.Groups.FirstOrDefaultAsync(g => g.GroupName == model.Name);
            if (existingGroup != null)
            {
                return BadRequest("A group with this name already exists");
            }

            // Create new group
            var group = new Group
            {
                GroupName = model.Name,
                Description = model.Description,
                CreatorId = currentUser.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Groups.Add(group);
            
            // Add current user as admin member
            var membership = new GroupMember
            {
                Group = group,
                User = currentUser,
                Role = "Admin",
                JoinedAt = DateTime.UtcNow
            };
            
            _context.GroupMembers.Add(membership);
            await _context.SaveChangesAsync();

            return Ok(new { GroupId = group.GroupId });
        }

        [HttpGet]
        public async Task<IActionResult> GetUserGroups()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var groups = await _context.GroupMembers
                .Where(gm => gm.User.Id == currentUser.Id)
                .Select(gm => new
                {
                    Id = gm.Group.GroupId,
                    Name = gm.Group.GroupName,
                    Role = gm.Role,
                    UnreadCount = _context.Messages
                        .Count(m => 
                            m.ReceiverGroupId == gm.GroupId && 
                            m.SenderId != currentUser.Id && 
                            !_context.Messages.Any(rm => 
                                rm.MessageId == m.MessageId && 
                                rm.Status == "Read"))
                })
                .ToListAsync();

            return Json(groups);
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
            {
                return BadRequest("Display name is required");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var users = await _context.Users
                .Where(u => u.DisplayName.Contains(displayName) && u.Id != currentUser.Id)
                .Select(u => new
                {
                    Id = u.Id,
                    DisplayName = u.DisplayName,
                    // Check if there's already a friendship between the users
                    FriendshipStatus = _context.Friendships
                        .Where(f => 
                            (f.UserId == currentUser.Id && f.FriendId == u.Id) || 
                            (f.UserId == u.Id && f.FriendId == currentUser.Id))
                        .Select(f => f.Status)
                        .FirstOrDefault()
                })
                .Take(20) // Limit results for performance
                .ToListAsync();

            return Json(users);
        }

        [HttpPost]
        public async Task<IActionResult> SendFriendRequest(string friendId)
        {
            if (string.IsNullOrEmpty(friendId))
            {
                return BadRequest("Friend ID is required");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // Check if the friend exists
            var friend = await _userManager.FindByIdAsync(friendId);
            if (friend == null)
            {
                return NotFound("User not found");
            }

            // Don't allow sending requests to yourself
            if (friendId == currentUser.Id)
            {
                return BadRequest("You cannot send a friend request to yourself");
            }

            // Check if there's already a friendship between the users
            var existingFriendship = await _context.Friendships
                .FirstOrDefaultAsync(f => 
                    (f.UserId == currentUser.Id && f.FriendId == friendId) || 
                    (f.UserId == friendId && f.FriendId == currentUser.Id));

            if (existingFriendship != null)
            {
                return BadRequest($"A friendship already exists with status: {existingFriendship.Status}");
            }

            // Create the new friendship
            var friendship = new Friendship
            {
                UserId = currentUser.Id,
                FriendId = friendId,
                Status = "Pending",
                RequestedAt = DateTime.UtcNow
            };

            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();

            // Send real-time notification to the recipient
            await _hubContext.Clients.User(friendId).SendAsync("FriendRequestReceived", new
            {
                RequestId = friendship.FriendshipId,
                FromUserId = currentUser.Id,
                FromUserName = currentUser.DisplayName
            });

            return Ok(new { FriendshipId = friendship.FriendshipId });
        }

        [HttpPost]
        public async Task<IActionResult> RespondToFriendRequest(int friendshipId, bool accept)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.FriendshipId == friendshipId && f.FriendId == currentUser.Id);

            if (friendship == null)
            {
                return NotFound("Friend request not found");
            }

            if (friendship.Status != "Pending")
            {
                return BadRequest("This friend request has already been processed");
            }

            if (accept)
            {
                // Accept the friend request
                friendship.Status = "Accepted";
                friendship.AcceptedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Notify the user who sent the request
                await _hubContext.Clients.User(friendship.UserId).SendAsync("FriendRequestAccepted", new
                {
                    FriendshipId = friendship.FriendshipId,
                    FriendId = currentUser.Id,
                    FriendName = currentUser.DisplayName
                });

                return Ok("Friend request accepted");
            }
            else
            {
                // Reject the friend request
                _context.Friendships.Remove(friendship);
                await _context.SaveChangesAsync();

                // Notify the user who sent the request
                await _hubContext.Clients.User(friendship.UserId).SendAsync("FriendRequestRejected", new
                {
                    FriendshipId = friendship.FriendshipId,
                    FriendId = currentUser.Id,
                    FriendName = currentUser.DisplayName
                });

                return Ok("Friend request rejected");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFriends()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // First, ensure we have the current user's status
            var userStatus = await _context.UserStatuses.FirstOrDefaultAsync(us => us.UserId == currentUser.Id);
            if (userStatus == null)
            {
                // Create status if it doesn't exist
                userStatus = new UserStatus
                {
                    UserId = currentUser.Id,
                    IsOnline = true,
                    LastSeen = DateTime.UtcNow
                };
                _context.UserStatuses.Add(userStatus);
                await _context.SaveChangesAsync();
            }

            // Get base friendship data without relying on complex navigation properties
            var friendships = await _context.Friendships
                .AsNoTracking()
                .Where(f => 
                    ((f.UserId == currentUser.Id) || (f.FriendId == currentUser.Id)) && 
                    f.Status == "Accepted")
                .Select(f => new
                {
                    FriendshipId = f.FriendshipId,
                    UserId = f.UserId == currentUser.Id ? f.FriendId : f.UserId,
                    AcceptedAt = f.AcceptedAt
                })
                .ToListAsync();
                
            // If no friends, return early
            if (!friendships.Any())
            {
                return Json(new List<object>());
            }
            
            // Get friend IDs
            var friendIds = friendships.Select(f => f.UserId).ToList();
            
            // Fetch friend details
            var friendUsers = await _context.Users
                .AsNoTracking()
                .Where(u => friendIds.Contains(u.Id))
                .Select(u => new 
                {
                    u.Id,
                    u.DisplayName
                })
                .ToListAsync();
                
            // Fetch status separately to avoid navigation property issues
            var friendStatuses = await _context.UserStatuses
                .AsNoTracking()
                .Where(us => friendIds.Contains(us.UserId))
                .ToListAsync();
                
            // Create a lookup for quick access to user details
            var userDict = friendUsers.ToDictionary(u => u.Id);
            var statusDict = friendStatuses.ToDictionary(s => s.UserId);
            
            // Assemble the complete friend data
            var friends = friendships.Select(f => new
            {
                FriendshipId = f.FriendshipId,
                UserId = f.UserId,
                DisplayName = userDict.TryGetValue(f.UserId, out var user) ? user.DisplayName : "Unknown User",
                AcceptedAt = f.AcceptedAt,
                IsOnline = statusDict.TryGetValue(f.UserId, out var status) && status.IsOnline,
                LastSeen = statusDict.TryGetValue(f.UserId, out var s) ? s.LastSeen : DateTime.MinValue
            }).ToList();

            return Json(friends);
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingFriendRequests()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // Get all pending friend requests where the current user is the recipient
            var pendingRequests = await _context.Friendships
                .Where(f => f.FriendId == currentUser.Id && f.Status == "Pending")
                .Select(f => new
                {
                    FriendshipId = f.FriendshipId,
                    RequesterId = f.UserId,
                    RequesterName = f.User.DisplayName,
                    RequestedAt = f.RequestedAt
                })
                .ToListAsync();

            return Json(pendingRequests);
        }

        [HttpPost]
        public async Task<IActionResult> AddUserToGroup(int groupId, string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // Check if the current user is an admin of the group
            var currentUserMembership = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == currentUser.Id);
            
            if (currentUserMembership == null || currentUserMembership.Role != "Admin")
            {
                return Forbid("Only group administrators can add members");
            }

            // Check if the user to be added exists
            var userToAdd = await _userManager.FindByIdAsync(userId);
            if (userToAdd == null)
            {
                return NotFound("User not found");
            }

            // Check if user is already a member
            var existingMembership = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
            
            if (existingMembership != null)
            {
                return BadRequest("User is already a member of this group");
            }

            // Add the new member
            var newMembership = new GroupMember
            {
                GroupId = groupId,
                UserId = userId,
                Role = "Member", // Default role is Member
                JoinedAt = DateTime.UtcNow
            };

            _context.GroupMembers.Add(newMembership);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.Group($"group_{groupId}").SendAsync("GroupMembersUpdated", groupId);

            // Get the group name for notifications
            var group = await _context.Groups.FindAsync(groupId);
            await _hubContext.Clients.User(userId).SendAsync("UserAddedToGroup", new {
                groupId,
                groupName = group?.GroupName ?? ""
            });

            // Notify group members about the new member
            await _hubContext.Clients.Group($"group_{groupId}").SendAsync("UserAddedToGroup", new
            {
                groupId = groupId,
                groupName = group.GroupName,
                userId = userId,
                displayName = userToAdd.DisplayName
            });

            return Ok(new { 
                groupId = groupId, 
                userId = userId,
                displayName = userToAdd.DisplayName
            });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveUserFromGroup(int groupId, string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // Check if the current user is an admin of the group
            var currentUserMembership = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == currentUser.Id);

            // Allow admins to remove any user or allow users to remove themselves
            if ((currentUserMembership == null || currentUserMembership.Role != "Admin") && 
                currentUser.Id != userId)
            {
                return Forbid("Only group administrators can remove other members");
            }

            // Find the membership to remove
            var membershipToRemove = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

            if (membershipToRemove == null)
            {
                return NotFound("User is not a member of this group");
            }

            // Don't allow removing the last admin
            if (membershipToRemove.Role == "Admin")
            {
                var adminCount = await _context.GroupMembers
                    .CountAsync(gm => gm.GroupId == groupId && gm.Role == "Admin");
                
                if (adminCount <= 1)
                {
                    return BadRequest("Cannot remove the last administrator");
                }
            }

            _context.GroupMembers.Remove(membershipToRemove);
            await _context.SaveChangesAsync();

            // Get the group name for notifications
            var group = await _context.Groups.FindAsync(groupId);
            if (group != null)
            {
                // 1. Notify all members that someone was removed
                await _hubContext.Clients.Group($"group_{groupId}").SendAsync("UserRemovedFromGroup", new
                {
                    groupId = groupId,
                    groupName = group.GroupName,
                    userId = userId
                });

                // 2. Notify all members to reload group members list
                await _hubContext.Clients.Group($"group_{groupId}").SendAsync("GroupMembersUpdated", groupId);

                // 3. Remove user khỏi SignalR group (all connectionIds)
                if (ChatHub.UserConnections.TryGetValue(userId, out var connIds))
                {
                    foreach (var connId in connIds)
                    {
                        await _hubContext.Groups.RemoveFromGroupAsync(connId, $"group_{groupId}");
                        // Notify riêng user bị kick (có thể handle UI ở client)
                        await _hubContext.Clients.Client(connId).SendAsync("YouWereRemovedFromGroup", new
                        {
                            groupId = groupId,
                            groupName = group.GroupName
                        });
                    }
                }
            }


            return Ok();
        }


        [HttpPost]
        public async Task<IActionResult> ChangeGroupMemberRole(int groupId, string userId, string role)
        {
            if (role != "Admin" && role != "Member")
            {
                return BadRequest("Invalid role specified");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // Check if the current user is an admin of the group
            var currentUserMembership = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == currentUser.Id);
            
            if (currentUserMembership == null || currentUserMembership.Role != "Admin")
            {
                return Forbid("Only group administrators can change member roles");
            }

            // Find the membership to update
            var membershipToUpdate = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
            
            if (membershipToUpdate == null)
            {
                return NotFound("User is not a member of this group");
            }

            // Don't allow demoting yourself if you're the last admin
            if (currentUser.Id == userId && role == "Member")
            {
                var adminCount = await _context.GroupMembers
                    .CountAsync(gm => gm.GroupId == groupId && gm.Role == "Admin");
                
                if (adminCount <= 1)
                {
                    return BadRequest("Cannot demote the last administrator");
                }
            }

            // Update the role
            membershipToUpdate.Role = role;
            await _context.SaveChangesAsync();

            // Get the user display name for notifications
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                // Notify group members about the role change
                await _hubContext.Clients.Group($"group_{groupId}").SendAsync("GroupMemberRoleChanged", new
                {
                    groupId = groupId,
                    userId = userId,
                    displayName = user.DisplayName,
                    newRole = role
                });
            }

            return Ok(new { 
                groupId = groupId,
                userId = userId,
                role = role
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetGroupMembers(int groupId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // Check if the current user is a member of the group
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == currentUser.Id);
            
            if (!isMember)
            {
                return Forbid("You must be a member of the group to view its members");
            }

            // Get all members with their roles and status
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

            return Json(members);
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsersForGroupInvite(int groupId, string query)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // Check if the current user is an admin of the group
            var currentUserMembership = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == currentUser.Id);
            
            if (currentUserMembership == null || currentUserMembership.Role != "Admin")
            {
                return Forbid("Only group administrators can invite users");
            }

            // Get existing members to exclude them
            var existingMemberIds = await _context.GroupMembers
                .Where(gm => gm.GroupId == groupId)
                .Select(gm => gm.UserId)
                .ToListAsync();

            // Search for users by display name or username who are not already in the group
            var users = await _userManager.Users
                .Where(u => 
                    !existingMemberIds.Contains(u.Id) && 
                    (u.DisplayName.Contains(query) || u.UserName.Contains(query)))
                .Select(u => new
                {
                    userId = u.Id,
                    userName = u.UserName,
                    displayName = u.DisplayName
                })
                .Take(10)
                .ToListAsync();

            return Json(users);
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsersForRoomInvite(string roomName, string query)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(query) || query.Length < 2)
            {
                return Json(new List<object>());
            }

            // Get the room group
            var room = await _context.Groups.FirstOrDefaultAsync(g => g.GroupName == roomName);
            if (room == null)
            {
                return NotFound("Room not found");
            }

            // Get existing members to exclude them
            var existingMemberIds = await _context.GroupMembers
                .Where(gm => gm.GroupId == room.GroupId)
                .Select(gm => gm.UserId)
                .ToListAsync();

            // Add users who are currently in the room via SignalR but not as permanent members
            var hubContext = (IHubContext<ChatHub>)HttpContext.RequestServices.GetService(typeof(IHubContext<ChatHub>));
            var onlineUsers = ChatHub.GetOnlineUsers();
            var usersInRoom = onlineUsers.Where(u => u.CurrentRoom == roomName).Select(u => u.UserId).ToList();
            existingMemberIds.AddRange(usersInRoom.Except(existingMemberIds));

            // Search for users by display name or username who are not already in the room
            var users = await _userManager.Users
                .Where(u => 
                    !existingMemberIds.Contains(u.Id) && 
                    (u.UserName.Contains(query) || u.DisplayName.Contains(query)))
                .Select(u => new
                {
                    userId = u.Id,
                    userName = u.UserName,
                    displayName = u.DisplayName
                })
                .Take(10)
                .ToListAsync();

            return Json(users);
        }

        [HttpPost]
        public async Task<IActionResult> InviteUserToRoom(string roomName, string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // Check if the room exists
            var room = await _context.Groups.FirstOrDefaultAsync(g => g.GroupName == roomName);
            if (room == null)
            {
                return NotFound("Room not found");
            }

            // Check if the invited user exists
            var userToInvite = await _userManager.FindByIdAsync(userId);
            if (userToInvite == null)
            {
                return NotFound("User not found");
            }
            
            // Check if user is already a member of the room
            var existingMembership = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == room.GroupId && gm.UserId == userId);
                
            if (existingMembership)
            {
                return BadRequest("User is already a member of this room");
            }

            // Notify the user about the invitation via SignalR
            var hubContext = (IHubContext<ChatHub>)HttpContext.RequestServices.GetService(typeof(IHubContext<ChatHub>));
            
            await hubContext.Clients.User(userId).SendAsync("RoomInviteReceived", new
            {
                roomName = roomName,
                roomId = room.GroupId,
                inviterId = currentUser.Id,
                inviterName = currentUser.DisplayName
            });

            return Ok(new { 
                roomId = room.GroupId, 
                userId = userId,
                displayName = userToInvite.DisplayName
            });
        }
    }

    public class SendMessageViewModel
    {
        public string Content { get; set; }
        public string ReceiverUserId { get; set; }
        public int? ReceiverGroupId { get; set; }
        public string RoomName { get; set; }
    }

    public class CreateRoomViewModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class CreateGroupViewModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}