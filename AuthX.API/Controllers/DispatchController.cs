using AuthX.Core.Constants;
using AuthX.Core.DTOs.Common;
using AuthX.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthX.API.Controllers;

public class DispatchController : BaseController
{
    private readonly IDispatchService _svc;
    public DispatchController(IDispatchService svc) => _svc = svc;

    /// <summary>Scan QR to dispatch (activates warranty)</summary>
    [HttpPost("scan")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Warehouse}")]
    public async Task<IActionResult> Scan(
        [FromQuery] string qrCode,
        [FromQuery] string? location)
        => OkResult(await _svc.ScanDispatchAsync(
            CurrentCompanyId, CurrentUserId, qrCode, location));

    /// <summary>Get dispatch history</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] long? batchId,
        [FromQuery] PaginationParams p)
        => OkResult(await _svc.GetDispatchesAsync(CurrentCompanyId, batchId, p));
}