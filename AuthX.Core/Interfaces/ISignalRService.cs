namespace AuthX.Core.Interfaces;

public interface ISignalRService
{
    Task PushToUserAsync(int userId, string eventName, object payload);
    Task PushToCompanyAsync(int companyId, string eventName, object payload);
    Task PushToGroupAsync(string groupName, string eventName, object payload);
}