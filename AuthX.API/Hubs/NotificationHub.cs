using AuthX.Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace AuthX.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId    = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var companyId = Context.User?.FindFirstValue("CompanyId");
        var roles     = Context.User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
                        ?? new List<string>();

        // Add to personal group
        if (!string.IsNullOrEmpty(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

        // Add to company group
        if (!string.IsNullOrEmpty(companyId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"company_{companyId}");

        // Add to role-based groups
        if (roles.Contains(AppRoles.Admin))
            await Groups.AddToGroupAsync(Context.ConnectionId, SignalRGroups.Admins);

        if (roles.Contains(AppRoles.Support) || roles.Contains(AppRoles.Admin))
            await Groups.AddToGroupAsync(Context.ConnectionId, SignalRGroups.SupportTeam);

        if (roles.Contains(AppRoles.Manager) || roles.Contains(AppRoles.Admin))
            await Groups.AddToGroupAsync(Context.ConnectionId, SignalRGroups.Managers);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    // Client can call this to mark notification read via hub
    public async Task MarkRead(long notificationId)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
            await Clients.Caller.SendAsync("NotificationRead", notificationId);
    }
}