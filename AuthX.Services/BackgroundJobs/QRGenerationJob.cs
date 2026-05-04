using AuthX.Core.Constants;
using AuthX.Core.Entities;
using AuthX.Core.Interfaces;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthX.Services.BackgroundJobs;

public class QRGenerationJob
{
    private readonly IUnitOfWork           _uow;
    private readonly INotificationService  _notif;
    private readonly ILogger<QRGenerationJob> _log;

    public QRGenerationJob(
        IUnitOfWork uow,
        INotificationService notif,
        ILogger<QRGenerationJob> log)
    {
        _uow   = uow;
        _notif = notif;
        _log   = log;
    }

    [Queue("qr_generation")]
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    public async Task GenerateAsync(int companyId, int userId, long batchId)
    {
        _log.LogInformation("QR Generation started — BatchId: {BatchId}", batchId);

        var batch = await _uow.Batches.FindOneAsync(b =>
            b.CompanyId == companyId && b.BatchId == batchId);

        if (batch == null)
        {
            _log.LogWarning("Batch {BatchId} not found. Job aborted.", batchId);
            return;
        }

        if (batch.Status != BatchStatuses.Draft)
        {
            _log.LogWarning("Batch {BatchId} already processed (status: {Status}). Skipping.",
                batchId, batch.Status);
            return;
        }

        try
        {
            var now   = DateTime.UtcNow;
            const int chunkSize = 5_000;
            int totalInserted = 0;

            // ── Generate in chunks — avoids memory spike for 500k items ──
            for (int chunkStart = 1; chunkStart <= batch.Quantity; chunkStart += chunkSize)
            {
                int chunkEnd = Math.Min(chunkStart + chunkSize - 1, batch.Quantity);
                var items = new List<ProductItem>(chunkEnd - chunkStart + 1);

                for (int i = chunkStart; i <= chunkEnd; i++)
                {
                    // ── FIX: NO TRUNCATION — full unique QR code ──────────────────
                    // Format: PV-{companyId}-{batchId}-{batchNo}-{sequence}-{guid}
                    // This is guaranteed unique because Guid.NewGuid() is UUID v4
                    var serial = $"{batch.BatchNo}-{i:D8}";
                    var qrCode = $"PV-{companyId}-{batchId}-{serial}-{Guid.NewGuid():N}".ToUpper();
                    // qrCode length: ~60-70 chars, well within VARCHAR(500)
                    // No truncation — no collision risk

                    items.Add(new ProductItem
                    {
                        CompanyId   = companyId,
                        ProductId   = batch.ProductId,
                        BatchId     = batchId,
                        SerialNo    = serial,
                        QRCode      = qrCode,
                        QRImagePath = null, // NOT storing images — on-demand generation
                        CreatedAt   = now
                    });
                }

                // ── Bulk insert with duplicate protection ──────────────────────────
                try
                {
                    await _uow.ProductItems.AddRangeAsync(items);
                    await _uow.SaveChangesAsync();
                    totalInserted += items.Count;

                    _log.LogInformation(
                        "Batch {BatchId}: inserted chunk {Start}-{End} ({Total}/{Grand} total)",
                        batchId, chunkStart, chunkEnd, totalInserted, batch.Quantity);
                }
                catch (DbUpdateException dbEx) when (IsUniqueConstraintViolation(dbEx))
                {
                    // Extremely rare — Guid collision (practically impossible but handle gracefully)
                    _log.LogError(dbEx,
                        "Batch {BatchId}: unique constraint on chunk {Start}-{End}. Retrying one-by-one.",
                        batchId, chunkStart, chunkEnd);

                    // Retry this chunk item-by-item to skip any duplicates
                    int recovered = await InsertOneByOneAsync(items, batchId);
                    totalInserted += recovered;
                }
            }

            // ── Log generation record ────────────────────────────────────────────
            await _uow.QRGenerations.AddAsync(new QRGeneration
            {
                CompanyId      = companyId,
                BatchId        = batchId,
                TotalGenerated = totalInserted,
                GeneratedBy    = userId
            });

            batch.Status = BatchStatuses.QRGenerated;
            _uow.Batches.Update(batch);
            await _uow.SaveChangesAsync();

            await _notif.PushAsync(
                companyId:   companyId,
                type:        NotificationTypes.QRGenerated,
                referenceId: batchId,
                message:     $"✓ {totalInserted:N0} QR codes generated for batch {batch.BatchNo}",
                actionUrl:   $"/batches/{batchId}");

            _log.LogInformation(
                "QR Generation complete — BatchId: {BatchId}, Total: {Total}",
                batchId, totalInserted);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "QR Generation failed for BatchId: {BatchId}", batchId);
            throw; // Hangfire retries
        }
    }

    private async Task<int> InsertOneByOneAsync(List<ProductItem> items, long batchId)
    {
        int inserted = 0;
        foreach (var item in items)
        {
            try
            {
                // Regenerate QR to avoid duplicate
                item.QRCode = $"PV-{item.CompanyId}-{batchId}-{item.SerialNo}-{Guid.NewGuid():N}".ToUpper();
                await _uow.ProductItems.AddAsync(item);
                await _uow.SaveChangesAsync();
                inserted++;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Skipping item {SerialNo} due to error.", item.SerialNo);
            }
        }
        return inserted;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains("UNIQUE") || msg.Contains("unique") ||
               msg.Contains("duplicate key") || msg.Contains("IX_");
    }
}