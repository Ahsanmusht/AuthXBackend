using AuthX.Core.Constants;
using AuthX.Core.Interfaces;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthX.Services.BackgroundJobs;

public class PrintProcessingJob
{
    private readonly IUnitOfWork              _uow;
    private readonly INotificationService     _notif;
    private readonly ILogger<PrintProcessingJob> _log;

    public PrintProcessingJob(
        IUnitOfWork uow,
        INotificationService notif,
        ILogger<PrintProcessingJob> log)
    {
        _uow   = uow;
        _notif = notif;
        _log   = log;
    }

    [Queue("print_jobs")]
    public async Task ProcessAsync(int companyId, int userId, long printJobId)
    {
        _log.LogInformation("Print job started — PrintJobId: {JobId}", printJobId);

        var job = await _uow.PrintJobs.FindOneAsync(j =>
            j.CompanyId == companyId && j.PrintJobId == printJobId);

        if (job == null)
        {
            _log.LogWarning("PrintJob {JobId} not found.", printJobId);
            return;
        }

        try
        {
            job.Status = "Processing";
            _uow.PrintJobs.Update(job);
            await _uow.SaveChangesAsync();

            // Mark all Pending items in batch as Printed — chunked
            const int chunk = 10_000;
            int offset = 0, totalPrinted = 0;

            while (true)
            {
                var items = await _uow.ProductItems.Query()
                    .Where(i => i.BatchId == job.BatchId &&
                                i.PrintStatus == "Pending" &&
                                i.CompanyId == companyId)
                    .OrderBy(i => i.ItemId)
                    .Skip(offset)
                    .Take(chunk)
                    .ToListAsync();

                if (!items.Any()) break;

                items.ForEach(i =>
                {
                    i.PrintStatus = "Printed";
                    i.Status      = ItemStatuses.Printed;
                    _uow.ProductItems.Update(i);
                });

                await _uow.SaveChangesAsync();

                totalPrinted += items.Count;
                offset       += chunk;

                _log.LogInformation(
                    "PrintJob {JobId}: marked {Count} items printed",
                    printJobId, totalPrinted);
            }

            // Update batch status
            var batch = await _uow.Batches.GetByIdAsync(job.BatchId);
            if (batch != null)
            {
                batch.Status = BatchStatuses.Printed;
                _uow.Batches.Update(batch);
            }

            job.PrintedCount = totalPrinted;
            job.Status       = "Done";
            job.CompletedAt  = DateTime.UtcNow;
            _uow.PrintJobs.Update(job);
            await _uow.SaveChangesAsync();

            await _notif.PushAsync(
                companyId:   companyId,
                type:        NotificationTypes.PrintDone,
                referenceId: printJobId,
                message:     $"✓ Print job complete — {totalPrinted:N0} items marked printed.",
                actionUrl:   $"/print-jobs/{printJobId}");

            _log.LogInformation(
                "PrintJob {JobId} complete. Total printed: {Total}",
                printJobId, totalPrinted);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "PrintJob {JobId} failed.", printJobId);
            job.Status       = "Failed";
            job.ErrorMessage = ex.Message[..Math.Min(ex.Message.Length, 499)];
            _uow.PrintJobs.Update(job);
            await _uow.SaveChangesAsync();
            throw;
        }
    }
}