using AuthX.Core.Constants;
using AuthX.Core.Entities;
using AuthX.Core.Interfaces;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthX.Services.BackgroundJobs;

public class QRGenerationJob
{
    private readonly IUnitOfWork              _uow;
    private readonly INotificationService     _notif;
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

        // ── Warranty mode fetch karo ────────────────────────────────────────
        var settings = await _uow.CompanySettings.Query()
            .FirstOrDefaultAsync(s => s.CompanyId == companyId);

        var warrantyMode  = settings?.WarrantyStartMode ?? "AfterDispatch";
        var warrantyDelay = settings?.WarrantyDelayDays ?? 60;

        // AfterQRGenerate mode mein: QR generate hote hi warranty set karo
        // Warranty start = now + delayDays, End = start + product.WarrantyDays
        bool setWarrantyOnGenerate = warrantyMode == "AfterQRGenerate";

        // Product warranty days chahiye agar AfterQRGenerate mode hai
        int productWarrantyDays = 365; // default
        if (setWarrantyOnGenerate)
        {
            var product = await _uow.Products.Query()
                .Where(p => p.ProductId == batch.ProductId)
                .Select(p => new { p.WarrantyDays })
                .FirstOrDefaultAsync();
            productWarrantyDays = product?.WarrantyDays ?? 365;
        }

        try
        {
            var now               = DateTime.UtcNow;
            var warrantyStartDate = now.AddDays(warrantyDelay);        // delay ke baad start
            var warrantyEndDate   = warrantyStartDate.AddDays(productWarrantyDays); // end

            const int chunkSize = 5_000;
            int totalInserted = 0;

            for (int chunkStart = 1; chunkStart <= batch.Quantity; chunkStart += chunkSize)
            {
                int chunkEnd = Math.Min(chunkStart + chunkSize - 1, batch.Quantity);
                var items    = new List<ProductItem>(chunkEnd - chunkStart + 1);

                for (int i = chunkStart; i <= chunkEnd; i++)
                {
                    var serial = $"{batch.BatchNo}-{i:D8}";
                    var qrCode = $"PV-{companyId}-{batchId}-{serial}-{Guid.NewGuid():N}".ToUpper();

                    var item = new ProductItem
                    {
                        CompanyId   = companyId,
                        ProductId   = batch.ProductId,
                        BatchId     = batchId,
                        SerialNo    = serial,
                        QRCode      = qrCode,
                        QRImagePath = null,
                        CreatedAt   = now
                    };

                    // ── AfterQRGenerate: warranty dates abhi set karo ──────
                    if (setWarrantyOnGenerate)
                    {
                        item.WarrantyStartDate = warrantyStartDate;
                        item.WarrantyEndDate   = warrantyEndDate;
                        item.IsFirstScan       = true;
                        item.FirstScanDate     = now;
                        item.FirstScanType     = "QRGenerate";
                        // Status still 'Generated' — dispatch ki zaroorat nahi warranty ke liye
                    }

                    items.Add(item);
                }

                try
                {
                    await _uow.ProductItems.AddRangeAsync(items);
                    await _uow.SaveChangesAsync();
                    totalInserted += items.Count;

                    _log.LogInformation(
                        "Batch {BatchId}: inserted chunk {Start}-{End} ({Total}/{Grand} total) | WarrantyMode: {Mode}",
                        batchId, chunkStart, chunkEnd, totalInserted, batch.Quantity, warrantyMode);
                }
                catch (DbUpdateException dbEx) when (IsUniqueConstraintViolation(dbEx))
                {
                    _log.LogError(dbEx,
                        "Batch {BatchId}: unique constraint on chunk {Start}-{End}. Retrying one-by-one.",
                        batchId, chunkStart, chunkEnd);

                    int recovered = await InsertOneByOneAsync(items, batchId, setWarrantyOnGenerate,
                        warrantyStartDate, warrantyEndDate, now);
                    totalInserted += recovered;
                }
            }

            // ── QRGeneration log ──────────────────────────────────────────
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

            // Notification message — mode ke hisab se
            var notifMsg = setWarrantyOnGenerate
                ? $"✓ {totalInserted:N0} QR codes generated for batch {batch.BatchNo}. Warranty auto-activated (starts {warrantyStartDate:dd MMM yyyy})."
                : $"✓ {totalInserted:N0} QR codes generated for batch {batch.BatchNo}. Warranty starts after dispatch.";

            await _notif.PushAsync(
                companyId:   companyId,
                type:        NotificationTypes.QRGenerated,
                referenceId: batchId,
                message:     notifMsg,
                actionUrl:   $"/batches/{batchId}");

            _log.LogInformation(
                "QR Generation complete — BatchId: {BatchId}, Total: {Total}, WarrantyMode: {Mode}",
                batchId, totalInserted, warrantyMode);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "QR Generation failed for BatchId: {BatchId}", batchId);
            throw; // Hangfire retries
        }
    }

    // ─── Retry one-by-one on unique constraint violation ─────────────────────
    private async Task<int> InsertOneByOneAsync(
        List<ProductItem> items,
        long batchId,
        bool setWarranty,
        DateTime warrantyStart,
        DateTime warrantyEnd,
        DateTime now)
    {
        int inserted = 0;
        foreach (var item in items)
        {
            try
            {
                item.QRCode = $"PV-{item.CompanyId}-{batchId}-{item.SerialNo}-{Guid.NewGuid():N}".ToUpper();

                if (setWarranty)
                {
                    item.WarrantyStartDate = warrantyStart;
                    item.WarrantyEndDate   = warrantyEnd;
                }

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