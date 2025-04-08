using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Data;
using Cloud9_2.Models;

namespace Cloud9_2.Hubs
{
    public class UserActivityHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public UserActivityHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task GetActiveUsers()
        {
            var activeUsers = _context.UserActivities
                .Where(a => a.IsActive)
                .Select(a => new { a.UserName, LoginTime = a.LoginTime.ToString("yyyy-MM-dd HH:mm:ss") })
                .ToList();
            await Clients.Caller.SendAsync("ReceiveActiveUsers", activeUsers);
        }

        public async Task GetOnlineUsernames()
        {
            var onlineUsernames = _context.UserActivities
                .Where(a => a.IsActive)
                .Select(a => a.UserName)
                .Distinct()
                .ToList();
            await Clients.Caller.SendAsync("ReceiveOnlineUsers", onlineUsernames);
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            await GetActiveUsers();
            await GetOnlineUsernames();
        }
    }
}