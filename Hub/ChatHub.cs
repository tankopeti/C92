using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Cloud9_2.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendPrivateMessage(string toUser, string message)
        {
            var sender = Context.User?.Identity?.Name ?? "Unknown";
            var receiverConnectionId = UserHandler.ConnectedUsers
                .FirstOrDefault(u => u.Key == toUser).Value;

            if (receiverConnectionId != null)
            {
                await Clients.Client(receiverConnectionId).SendAsync("ReceivePrivateMessage", sender, message, false);
                await Clients.Caller.SendAsync("ReceivePrivateMessage", sender, message, false);
            }
        }

        public async Task SendGroupMessage(string message)
        {
            var sender = Context.User?.Identity?.Name ?? "Unknown";
            await Clients.All.SendAsync("ReceivePrivateMessage", sender, message, true);
        }

        public override Task OnConnectedAsync()
        {
            var username = Context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                UserHandler.ConnectedUsers[username] = Context.ConnectionId;
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var username = Context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                UserHandler.ConnectedUsers.Remove(username);
            }
            return base.OnDisconnectedAsync(exception);
        }
    }

    public static class UserHandler
    {
        public static Dictionary<string, string> ConnectedUsers { get; set; } = new Dictionary<string, string>();
    }
}
