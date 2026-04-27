using AuthX.Core.Interfaces;
using AuthX.Infrastructure.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthX.Services.BackgroundJobs;

public class ScanLogCleanupJob
{
    private readonly AppDbContext _ctx;
    private readonly ILogger<ScanLogCleanupJob> _log;

    public ScanLogCleanupJob(AppDbContext ctx, ILogger<ScanLogCleanupJob> log)
    {
        _ctx = ctx;
        _log = log;
    }

    [Queue("default")]
    public async Task CleanOldLogsAsync(int olderThanDays)
    {
        var cutoff = DateTime.UtcNow.AddDays(-olderThanDays);
        
        // Direct SQL DELETE — no memory loading
        var deleted = await _ctx.Database.ExecuteSqlRawAsync(
            "DELETE FROM ScanLog WHERE ScanTime < {0}", cutoff);

        _log.LogInformation("ScanLog cleanup: removed {Count} old records.", deleted);
    }
}