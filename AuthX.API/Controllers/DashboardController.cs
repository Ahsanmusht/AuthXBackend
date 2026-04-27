using AuthX.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AuthX.API.Controllers;

public class DashboardController : BaseController
{
    private readonly IDashboardService _svc;
    public DashboardController(IDashboardService svc) => _svc = svc;

    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
        => OkResult(await _svc.GetStatsAsync(CurrentCompanyId));

    [HttpGet("scan-trend")]
    public async Task<IActionResult> ScanTrend([FromQuery] int days = 30)
        => OkResult(await _svc.GetScanTrendAsync(CurrentCompanyId, days));

    [HttpGet("claim-trend")]
    public async Task<IActionResult> ClaimTrend([FromQuery] int days = 30)
        => OkResult(await _svc.GetClaimTrendAsync(CurrentCompanyId, days));
}