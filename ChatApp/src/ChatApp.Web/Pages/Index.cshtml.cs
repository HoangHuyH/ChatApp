using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ChatApp.Web.Data;
using ChatApp.Web.Models.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApp.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDbContext _context;

    public IndexModel(
        ILogger<IndexModel> logger,
        UserManager<User> userManager,
        ApplicationDbContext context)
    {
        _logger = logger;
        _userManager = userManager;
        _context = context;
    }

    public string? CurrentUserId { get; set; }
    public List<User> Friends { get; set; } = new();
    public List<GroupViewModel> Groups { get; set; } = new();
    public string? SelectedUserId { get; set; }
    public string? SelectedChatName { get; set; }

    public async Task OnGetAsync(string? userId = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                CurrentUserId = currentUser.Id;
                
                // Get friends
                var friendships = await _context.Friendships
                    .Where(f => (f.UserId == currentUser.Id || f.FriendId == currentUser.Id) && 
                                f.Status == "Accepted")
                    .ToListAsync();
                
                var friendIds = friendships
                    .Select(f => f.UserId == currentUser.Id ? f.FriendId : f.UserId)
                    .ToList();
                
                Friends = await _userManager.Users
                    .Where(u => friendIds.Contains(u.Id))
                    .Include(u => u.Status)
                    .ToListAsync();
                
                // Get groups
                var userGroups = await _context.GroupMembers
                    .Where(gm => gm.User.Id == currentUser.Id)
                    .Include(gm => gm.Group)
                    .ToListAsync();
                
                Groups = userGroups.Select(gm => new GroupViewModel
                {
                    GroupId = gm.GroupId,
                    GroupName = gm.Group.GroupName,
                    MemberCount = _context.GroupMembers.Count(m => m.GroupId == gm.GroupId)
                }).ToList();
                
                // If a specific user is selected
                if (!string.IsNullOrEmpty(userId))
                {
                    SelectedUserId = userId;
                    var selectedFriend = Friends.FirstOrDefault(f => f.Id == userId);
                    if (selectedFriend != null)
                    {
                        SelectedChatName = selectedFriend.DisplayName;
                    }
                }
            }
        }
    }
    
    // Helper ViewModel for Groups
    public class GroupViewModel
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
    }
}
