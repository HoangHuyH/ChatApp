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
            var rooms = await _context.Groups
                .Select(r => new {
                    Id = r.GroupId,
                    Name = r.GroupName,
                    CreatedBy = r.CreatorId,
                    MemberCount = r.Members.Count
                })
                .ToListAsync();

            return Json(rooms);
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
}