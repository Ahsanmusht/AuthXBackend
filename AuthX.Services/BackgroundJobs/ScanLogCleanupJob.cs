using AuthX.Core.Interfaces;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthX.Services.BackgroundJobs;

public class ScanLogCleanupJob
{
    private readonly IUnitOfWork              _uow;
    private readonly ILogger<ScanLogCleanupJob> _log;

    public ScanLogCleanupJob(IUnitOfWork uow, ILogger<ScanLogCleanupJob> log)
    {
        _uow = uow;
        _log = log;
    }

    [Queue("default")]
    public async Task CleanOldLogsAsync(int olderThanDays)
    {
        var cutoff = DateTime.UtcNow.AddDays(-olderThanDays);
        var old    = await _uow.ScanLogs.Query()
            .Where(s => s.ScanTime < cutoff)
            .ToListAsync();

        if (!old.Any())
        {
            _log.LogInformation("ScanLog cleanup: nothing to remove.");
            return;
        }

        old.ForEach(s => _uow.ScanLogs.Remove(s));
        await _uow.SaveChangesAsync();

        _log.LogInformation("ScanLog cleanup: removed {Count} old records.", old.Count);
    }
}