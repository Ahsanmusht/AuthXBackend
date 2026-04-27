using Hangfire;

namespace AuthX.Services.BackgroundJobs;

public static class JobScheduler
{
    public static void RegisterRecurringJobs()
    {
        // Clean old scan logs older than 6 months — runs daily at 2 AM
        RecurringJob.AddOrUpdate<ScanLogCleanupJob>(
            recurringJobId: "scan-log-cleanup",
            queue:          "default",
            methodCall:     job => job.CleanOldLogsAsync(180),
            cronExpression: "0 2 * * *");
    }
}