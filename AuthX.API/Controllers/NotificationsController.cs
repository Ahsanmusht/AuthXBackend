using AuthX.Core.DTOs.Common;
using AuthX.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AuthX.API.Controllers;

public class NotificationsController : BaseController
{
    private readonly INotificationService _svc;
    public NotificationsController(INotificationService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams p)
        => OkResult(await _svc.GetForUserAsync(CurrentUserId, p));

    [HttpPatch("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id)
    {
        await _svc.MarkReadAsync(CurrentUserId, id);
        return OkMessage("Marked as read.");
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await _svc.MarkAllReadAsync(CurrentUserId);
        return OkMessage("All marked as read.");
    }
}