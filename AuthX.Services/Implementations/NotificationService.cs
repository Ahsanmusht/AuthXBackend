using AuthX.Core.Constants;
using AuthX.Core.DTOs.Common;
using AuthX.Core.DTOs.Notifications;
using AuthX.Core.Entities;
using AuthX.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthX.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork    _uow;
    private readonly ISignalRService _signalR;

    public NotificationService(IUnitOfWork uow, ISignalRService signalR)
    {
        _uow      = uow;
        _signalR  = signalR;
    }

    public async Task<PagedResult<NotificationDto>> GetForUserAsync(int userId, PaginationParams p)
    {
        var query = _uow.Notifications.Query()
            .Where(n => n.TargetUserId == userId || n.TargetUserId == null);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((p.Page - 1) * p.PageSize)
            .Take(p.PageSize)
            .Select(n => new NotificationDto
            {
                NotificationId = n.NotificationId,
                Type           = n.Type,
                Message        = n.Message,
                ReferenceId    = n.ReferenceId,
                ActionUrl      = n.ActionUrl,
                IsRead         = n.IsRead,
                CreatedAt      = n.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<NotificationDto>
        {
            Items = items, TotalCount = total,
            Page  = p.Page, PageSize  = p.PageSize
        };
    }

    public async Task MarkReadAsync(int userId, long notificationId)
    {
        var notif = await _uow.Notifications.FindOneAsync(n =>
            n.NotificationId == notificationId &&
            (n.TargetUserId == userId || n.TargetUserId == null))
            ?? throw new KeyNotFoundException("Notification not found.");

        notif.IsRead = true;
        _uow.Notifications.Update(notif);
        await _uow.SaveChangesAsync();
    }

    public async Task MarkAllReadAsync(int userId)
    {
        var notifs = (await _uow.Notifications.FindAsync(n =>
            (n.TargetUserId == userId || n.TargetUserId == null) && !n.IsRead)).ToList();

        notifs.ForEach(n => { n.IsRead = true; _uow.Notifications.Update(n); });
        await _uow.SaveChangesAsync();
    }

    public async Task PushAsync(
        int companyId, string type, long? referenceId,
        string message, int? targetUserId = null,
        int? targetRoleId = null, string? actionUrl = null)
    {
        var notif = new Notification
        {
            CompanyId    = companyId,
            Type         = type,
            ReferenceId  = referenceId,
            Message      = message,
            TargetUserId = targetUserId,
            TargetRoleId = targetRoleId,
            ActionUrl    = actionUrl
        };

        await _uow.Notifications.AddAsync(notif);
        await _uow.SaveChangesAsync();

        // Real-time push
        var payload = new
        {
            notif.NotificationId,
            notif.Type,
            notif.Message,
            notif.ReferenceId,
            notif.ActionUrl,
            notif.CreatedAt
        };

        if (targetUserId.HasValue)
        {
            await _signalR.PushToUserAsync(targetUserId.Value, "NewNotification", payload);
        }
        else
        {
            // Push to role group
            var group = type switch
            {
                NotificationTypes.NewClaim     => SignalRGroups.SupportTeam,
                NotificationTypes.ClaimUpdated => SignalRGroups.SupportTeam,
                NotificationTypes.QRGenerated  => SignalRGroups.Managers,
                NotificationTypes.PrintDone    => SignalRGroups.Managers,
                _                              => $"company_{companyId}"
            };
            await _signalR.PushToGroupAsync(group, "NewNotification", payload);
        }
    }
}