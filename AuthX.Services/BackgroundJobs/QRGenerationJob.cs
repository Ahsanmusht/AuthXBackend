using AuthX.Core.Constants;
using AuthX.Core.Entities;
using AuthX.Core.Interfaces;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.QrCode.Internal;
using System.Drawing;
using System.Drawing.Imaging;

namespace AuthX.Services.BackgroundJobs;

public class QRGenerationJob
{
    private readonly IUnitOfWork           _uow;
    private readonly INotificationService  _notif;
    private readonly ILogger<QRGenerationJob> _log;

    // QR images store karne ki path — apni server path set karo
    private const string QR_BASE_PATH = "wwwroot/qr-images";

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

        // QR images folder create karo
        var qrFolder = Path.Combine(QR_BASE_PATH, companyId.ToString(), batchId.ToString());
        Directory.CreateDirectory(qrFolder);

        try
        {
            var now    = DateTime.UtcNow;
            var items  = new List<ProductItem>(batch.Quantity);

            for (int i = 1; i <= batch.Quantity; i++)
            {
                var serial = $"{batch.BatchNo}-{i:D8}";
                // QR Code string — ye customer scan krega
                var qrCode = $"PV-{companyId}-{batchId}-{serial}".ToUpper();

                // QR Image generate karo (ZXing.Net)
                var qrImagePath = GenerateQRImage(qrCode, qrFolder, serial);

                items.Add(new ProductItem
                {
                    CompanyId    = companyId,
                    ProductId    = batch.ProductId,
                    BatchId      = batchId,
                    SerialNo     = serial,
                    QRCode       = qrCode,
                    QRImagePath  = qrImagePath,  // ✅ Ab null nahi hoga
                    CreatedAt    = now
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
            throw;
        }
    }

    /// <summary>
    /// ZXing.Net se QR image generate karo aur PNG save karo
    /// Returns relative URL path for storing in DB
    /// </summary>
    private string GenerateQRImage(string qrCode, string folderPath, string serial)
    {
        try
        {
            var writer = new QRCodeWriter();
            var hints = new Dictionary<EncodeHintType, object>
            {
                { EncodeHintType.ERROR_CORRECTION, ErrorCorrectionLevel.M },
                { EncodeHintType.MARGIN, 1 },
                { EncodeHintType.CHARACTER_SET, "UTF-8" }
            };

            // 200x200 pixel QR generate karo
            var bitMatrix = writer.encode(qrCode, BarcodeFormat.QR_CODE, 200, 200, hints);

            // PNG file save karo
            var fileName  = $"{serial}.png";
            var filePath  = Path.Combine(folderPath, fileName);

            using var bitmap = new Bitmap(200, 200);
            for (int x = 0; x < 200; x++)
            {
                for (int y = 0; y < 200; y++)
                {
                    bitmap.SetPixel(x, y, bitMatrix[x, y] ? Color.Black : Color.White);
                }
            }
            bitmap.Save(filePath, ImageFormat.Png);

            // DB mein store karne ke liye relative URL return karo
            // Frontend /qr-images/{companyId}/{batchId}/{serial}.png se access krega
            return $"/qr-images/{Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(filePath)))}/{Path.GetFileName(Path.GetDirectoryName(filePath))}/{fileName}";
        }
        catch (Exception ex)
        {
            // QR image fail ho gayi to null return karo, generation band mat karo
            return null!;
        }
    }
}