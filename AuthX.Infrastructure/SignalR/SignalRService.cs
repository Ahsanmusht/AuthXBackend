using AuthX.Core.Interfaces;
using AuthX.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AuthX.Infrastructure.SignalR;

public class SignalRService : ISignalRService
{
    private readonly IHubContext<NotificationHub> _hub;

    public SignalRService(IHubContext<NotificationHub> hub) => _hub = hub;

    public async Task PushToUserAsync(int userId, string eventName, object payload)
        => await _hub.Clients.Group($"user_{userId}").SendAsync(eventName, payload);

    public async Task PushToCompanyAsync(int companyId, string eventName, object payload)
        => await _hub.Clients.Group($"company_{companyId}").SendAsync(eventName, payload);

    public async Task PushToGroupAsync(string groupName, string eventName, object payload)
        => await _hub.Clients.Group(groupName).SendAsync(eventName, payload);
}