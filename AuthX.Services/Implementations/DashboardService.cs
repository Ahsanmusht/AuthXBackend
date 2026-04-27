using AuthX.Core.Constants;
using AuthX.Core.DTOs.Dashboard;
using AuthX.Core.Interfaces;
using AuthX.Infrastructure.Cache;
using Microsoft.EntityFrameworkCore;

namespace AuthX.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork        _uow;
    private readonly IRedisCacheService _cache;

    public DashboardService(IUnitOfWork uow, IRedisCacheService cache)
    {
        _uow   = uow;
        _cache = cache;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(int companyId)
    {
        var key    = CacheKeys.DashboardStats(companyId);
        var cached = await _cache.GetAsync<DashboardStatsDto>(key);
        if (cached != null) return cached;

        var today = DateTime.UtcNow.Date;

        var stats = new DashboardStatsDto
        {
            TotalProducts    = await _uow.Products.CountAsync(p => p.CompanyId == companyId && p.IsActive),
            TotalBatches     = await _uow.Batches.CountAsync(b => b.CompanyId == companyId),
            TotalQRGenerated = await _uow.ProductItems.CountAsync(i => i.CompanyId == companyId),
            TotalDispatched  = await _uow.ProductItems.CountAsync(i =>
                i.CompanyId == companyId && i.Status == ItemStatuses.Dispatched),
            TotalScans       = await _uow.ScanLogs.Query().LongCountAsync(),
            TotalClaims      = await _uow.Claims.CountAsync(c => c.CompanyId == companyId),
            OpenClaims       = await _uow.Claims.CountAsync(c =>
                c.CompanyId == companyId && c.Status == ClaimStatuses.Open),
            ResolvedClaims   = await _uow.Claims.CountAsync(c =>
                c.CompanyId == companyId && c.Status == ClaimStatuses.Delivered),
            TodayScans       = await _uow.ScanLogs.Query()
                .LongCountAsync(s => s.ScanTime >= today),
            TodayClaims      = await _uow.Claims.CountAsync(c =>
                c.CompanyId == companyId && c.ClaimDate >= today)
        };

        await _cache.SetAsync(key, stats, TimeSpan.FromMinutes(5));
        return stats;
    }

    public async Task<List<ScanTrendDto>> GetScanTrendAsync(int companyId, int days)
    {
        var from = DateTime.UtcNow.Date.AddDays(-days);

        return await _uow.ScanLogs.Query()
            .Where(s => s.ScanTime >= from)
            .GroupBy(s => s.ScanTime.Date)
            .Select(g => new ScanTrendDto
            {
                Date  = g.Key.ToString("yyyy-MM-dd"),
                Count = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync();
    }

    public async Task<List<ClaimTrendDto>> GetClaimTrendAsync(int companyId, int days)
    {
        var from = DateTime.UtcNow.Date.AddDays(-days);

        return await _uow.Claims.Query()
            .Where(c => c.CompanyId == companyId && c.ClaimDate >= from)
            .GroupBy(c => new { c.ClaimDate.Date, c.Status })
            .Select(g => new ClaimTrendDto
            {
                Date   = g.Key.Date.ToString("yyyy-MM-dd"),
                Count  = g.Count(),
                Status = g.Key.Status
            })
            .OrderBy(x => x.Date)
            .ToListAsync();
    }
}