using AuthX.API.Hubs;
using AuthX.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace AuthX.API.Services;

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