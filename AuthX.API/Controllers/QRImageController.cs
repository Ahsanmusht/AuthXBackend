using AuthX.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZXing;
using ZXing.QrCode;
using ZXing.QrCode.Internal;
using ZXing.Rendering;
using System.Drawing;
using System.Drawing.Imaging;

namespace AuthX.API.Controllers;

/// <summary>
/// On-demand QR image generation — enterprise approach.
/// 
/// WHY NO FILE STORAGE:
/// - 500k QR codes/day = ~2GB+ PNG files daily
/// - File system bottleneck at scale
/// - Backup/replication nightmare
/// - CDN caching handles repeated requests efficiently
/// 
/// HOW IT WORKS:
/// - QR code STRING is stored in database (tiny — ~70 chars)
/// - Image generated ON REQUEST using ZXing.Net (in-memory, ~2ms)
/// - Response cached by CDN/browser for 1 year (immutable)
/// - Result: zero storage cost, infinite scale
/// </summary>
[ApiController]
[Route("api/qr-image")]
public class QRImageController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<QRImageController> _log;

    public QRImageController(IUnitOfWork uow, ILogger<QRImageController> log)
    {
        _uow = uow;
        _log = log;
    }

    /// <summary>
    /// Get QR code image by QR code string.
    /// URL: GET /api/qr-image/by-code?q=PV-1-1-BATCH01-00000001-GUID
    /// 
    /// This endpoint is PUBLIC (no auth) because:
    /// - Customer's phone camera needs to scan it during dispatch printing
    /// - QR code itself IS the secret (UUID v4 — practically unguessable)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("by-code")]
    [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<IActionResult> GetByCode([FromQuery] string q, [FromQuery] int size = 200)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("QR code is required.");

        // Clamp size for security (prevent memory abuse)
        size = Math.Clamp(size, 50, 500);

        // Verify QR code exists in DB (optional security check — remove if too slow at scale)
        // For high-throughput scenarios, skip DB check and just generate the image
        // The QR string itself is the authority

        var imageBytes = GenerateQRImage(q, size);
        
        Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
        Response.Headers["ETag"] = $"\"{q.GetHashCode():X8}\"";
        
        return File(imageBytes, "image/png");
    }

    /// <summary>
    /// Get QR image by ItemId — for authenticated dashboard use.
    /// URL: GET /api/qr-image/{itemId}?size=200
    /// </summary>
    [HttpGet("{itemId:long}")]
    [Authorize]
    [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<IActionResult> GetById(long itemId, [FromQuery] int size = 200)
    {
        size = Math.Clamp(size, 50, 500);

        // Get QR code string from DB
        var item = await _uow.ProductItems.Query()
            .Where(i => i.ItemId == itemId)
            .Select(i => new { i.QRCode, i.CompanyId })
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound();

        var imageBytes = GenerateQRImage(item.QRCode, size);

        Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
        return File(imageBytes, "image/png");
    }

    /// <summary>
    /// Batch QR image generation for print preview — returns up to 50 QR images as JSON.
    /// Used by frontend label preview.
    /// URL: GET /api/qr-image/batch/{batchId}?page=1&pageSize=12
    /// </summary>
    [HttpGet("batch/{batchId:long}")]
    [Authorize]
    public async Task<IActionResult> GetBatchPreview(
        long batchId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        page     = Math.Max(1, page);

        var items = await _uow.ProductItems.Query()
            .Where(i => i.BatchId == batchId)
            .OrderBy(i => i.ItemId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new { i.ItemId, i.SerialNo, i.QRCode })
            .ToListAsync();

        // Return QR code STRINGS — let frontend render them
        // Frontend uses a QR library (e.g. qrcode.react) to render client-side
        // This avoids server-side image generation for preview
        return Ok(new
        {
            success = true,
            data = items.Select(i => new
            {
                i.ItemId,
                i.SerialNo,
                i.QRCode,
                // Image URL — frontend can use this OR render client-side
                ImageUrl = Url.Action("GetByCode", "QRImage",
                    new { q = i.QRCode, size = 150 }, Request.Scheme)
            })
        });
    }

    // ─── Private: ZXing QR Generation ─────────────────────────────────────────
    
    private static byte[] GenerateQRImage(string content, int size)
    {
        var writer = new QRCodeWriter();
        var hints  = new Dictionary<EncodeHintType, object>
        {
            [EncodeHintType.ERROR_CORRECTION] = ErrorCorrectionLevel.M,
            [EncodeHintType.MARGIN]           = 1, // Minimal quiet zone
            [EncodeHintType.CHARACTER_SET]    = "UTF-8"
        };

        var bitMatrix = writer.encode(content, BarcodeFormat.QR_CODE, size, size, hints);

        // Convert BitMatrix to PNG bytes
        using var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                bitmap.SetPixel(x, y, bitMatrix[x, y] ? Color.Black : Color.White);
            }
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}