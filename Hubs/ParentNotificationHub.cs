using Microsoft.AspNetCore.SignalR;

namespace Group3_SE1902_PRN222_LibraryManagement.Hubs
{
    /// <summary>
    /// SignalR hub for real-time parent notifications.
    /// Clients join a group named "parent_{userId}" on connect.
    /// </summary>
    public class ParentNotificationHub : Hub
    {
        public async Task JoinParentGroup(string parentId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"parent_{parentId}");
        }

        public async Task LeaveParentGroup(string parentId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"parent_{parentId}");
        }
    }
}
