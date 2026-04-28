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
            _log.LogWarning("Batch {BatchId} already processed.", batchId);
            return;
        }

        try
        {
            var now    = DateTime.UtcNow;
            var items  = new List<ProductItem>(batch.Quantity);

            for (int i = 1; i <= batch.Quantity; i++)
            {
                var serial = $"{batch.BatchNo}-{i:D8}";
                var qrCode = $"PV-{companyId}-{batchId}-{serial}-{Guid.NewGuid():N}"[..48].ToUpper();

                items.Add(new ProductItem
                {
                    CompanyId = companyId,
                    ProductId = batch.ProductId,
                    BatchId   = batchId,
                    SerialNo  = serial,
                    QRCode    = qrCode,
                    QRImagePath = null,
                    CreatedAt = now
                });
            }

            // Chunked insert: 5,000 per transaction
            const int chunk = 5_000;
            for (int i = 0; i < items.Count; i += chunk)
            {
                var slice = items.Skip(i).Take(chunk).ToList();
                await _uow.ProductItems.AddRangeAsync(slice);
                await _uow.SaveChangesAsync();

                _log.LogInformation(
                    "Batch {BatchId}: inserted {Count}/{Total}",
                    batchId, Math.Min(i + chunk, items.Count), items.Count);
            }

            // Log generation record
            await _uow.QRGenerations.AddAsync(new QRGeneration
            {
                CompanyId      = companyId,
                BatchId        = batchId,
                TotalGenerated = batch.Quantity,
                GeneratedBy    = userId
            });

            batch.Status = BatchStatuses.QRGenerated;
            _uow.Batches.Update(batch);
            await _uow.SaveChangesAsync();

            await _notif.PushAsync(
                companyId:   companyId,
                type:        NotificationTypes.QRGenerated,
                referenceId: batchId,
                message:     $"✓ {batch.Quantity:N0} QR codes generated for batch {batch.BatchNo}",
                actionUrl:   $"/batches/{batchId}");

            _log.LogInformation(
                "QR Generation complete — BatchId: {BatchId}, Total: {Total}",
                batchId, batch.Quantity);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "QR Generation failed for BatchId: {BatchId}", batchId);
            throw; // Hangfire will retry
        }
    }
}