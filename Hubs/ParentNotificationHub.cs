using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Group3_SE1902_PRN222_LibraryManagement.Hubs;

[Authorize(Roles = "Parent")]
public sealed class ParentNotificationHub : Hub
{
}
