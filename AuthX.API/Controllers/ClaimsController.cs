using AuthX.Core.Constants;
using AuthX.Core.DTOs.Claims;
using AuthX.Core.DTOs.Common;
using AuthX.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthX.API.Controllers;

public class ClaimsController : BaseController
{
    private readonly IClaimService _svc;
    public ClaimsController(IClaimService svc) => _svc = svc;

    /// <summary>
    /// Public QR scan endpoint — no auth required.
    /// Customer scans QR, gets product genuineness info.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("scan")]
    public async Task<IActionResult> Scan([FromQuery] string qrCode)
    {
        var ip         = HttpContext.Connection.RemoteIpAddress?.ToString();
        var deviceInfo = Request.Headers["User-Agent"].ToString();
        decimal? lat   = null, lon = null;

        if (Request.Headers.TryGetValue("X-Latitude",  out var latVal))
            decimal.TryParse(latVal, out var latParsed).Let(_ => lat = latParsed);
        if (Request.Headers.TryGetValue("X-Longitude", out var lonVal))
            decimal.TryParse(lonVal, out var lonParsed).Let(_ => lon = lonParsed);

        var result = await _svc.ScanQRAsync(qrCode, ip, deviceInfo, lat, lon);
        return OkResult(result);
    }

    /// <summary>Customer submits a warranty claim (public)</summary>
    [AllowAnonymous]
    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] SubmitClaimDto dto)
        => OkResult(await _svc.SubmitClaimAsync(dto));

    /// <summary>Get all claims (support/admin)</summary>
    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager},{AppRoles.Support}")]
    public async Task<IActionResult> GetAll(
        [FromQuery] ClaimFilterDto filter,
        [FromQuery] PaginationParams p)
        => OkResult(await _svc.GetClaimsAsync(CurrentCompanyId, filter, p));

    /// <summary>Get claim detail</summary>
    [HttpGet("{id:long}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager},{AppRoles.Support}")]
    public async Task<IActionResult> GetById(long id)
        => OkResult(await _svc.GetByIdAsync(CurrentCompanyId, id));

    /// <summary>Update claim status</summary>
    [HttpPatch("{id:long}/status")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager},{AppRoles.Support}")]
    public async Task<IActionResult> UpdateStatus(
        long id, [FromBody] UpdateClaimStatusDto dto)
        => OkResult(await _svc.UpdateStatusAsync(
            CurrentCompanyId, id, CurrentUserId, dto));
}