using AuthX.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZXing;
using ZXing.QrCode;
using ZXing.QrCode.Internal;
using ZXing.ImageSharp.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using ZXing.Common;

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
    private readonly IConfiguration _configuration;
    private readonly string _baseUrl;

    public QRImageController(IUnitOfWork uow, ILogger<QRImageController> log, IConfiguration configuration)
    {
        _uow = uow;
        _log = log;
        _configuration = configuration;
        _baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://devapi.authx.pk";
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
        page = Math.Max(1, page);

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
                //     ImageUrl = Url.Action("GetByCode", "QRImage",
                //         new { q = i.QRCode, size = 150 }, Request.Scheme)
                ImageUrl = $"{_baseUrl}/api/qr-image/by-code?q={i.QRCode}&size=200"
            })
        });
    }

    // ─── Private: ZXing QR Generation ─────────────────────────────────────────

    private static byte[] GenerateQRImage(string content, int size)
    {
        var writer = new ZXing.BarcodeWriterPixelData
        {
            Format = ZXing.BarcodeFormat.QR_CODE,
            Options = new ZXing.Common.EncodingOptions
            {
                Width = size,
                Height = size,
                Margin = 2,
                PureBarcode = false
            }
        };

        var pixelData = writer.Write(content);
        return CreatePng(pixelData.Pixels, pixelData.Width, pixelData.Height);
    }

    private static byte[] CreatePng(byte[] rgba, int w, int h)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // PNG signature
        bw.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

        // IHDR
        WriteChunk(bw, "IHDR", b =>
        {
            b.Write(ToBE(w));
            b.Write(ToBE(h));
            b.Write((byte)8);  // bit depth
            b.Write((byte)2);  // RGB
            b.Write((byte)0); b.Write((byte)0); b.Write((byte)0);
        });

        // IDAT - raw RGB scanlines
        var raw = new byte[h * (1 + w * 3)];
        for (int y = 0; y < h; y++)
        {
            raw[y * (w * 3 + 1)] = 0; // filter byte
            for (int x = 0; x < w; x++)
            {
                int src = (y * w + x) * 4;
                int dst = y * (w * 3 + 1) + 1 + x * 3;
                raw[dst] = rgba[src];     // R
                raw[dst + 1] = rgba[src + 1]; // G
                raw[dst + 2] = rgba[src + 2]; // B
            }
        }

        using var compressed = new MemoryStream();
        using (var zlib = new System.IO.Compression.ZLibStream(
            compressed, System.IO.Compression.CompressionLevel.Fastest, true))
            zlib.Write(raw, 0, raw.Length);

        WriteChunk(bw, "IDAT", b => b.Write(compressed.ToArray()));
        WriteChunk(bw, "IEND", _ => { });

        return ms.ToArray();
    }

    private static void WriteChunk(BinaryWriter bw, string type, Action<BinaryWriter> data)
    {
        using var buf = new MemoryStream();
        using var tmp = new BinaryWriter(buf);
        data(tmp);
        var bytes = buf.ToArray();
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);

        bw.Write(ToBE(bytes.Length));
        bw.Write(typeBytes);
        bw.Write(bytes);
        bw.Write(ToBE((int)Crc32(typeBytes.Concat(bytes).ToArray())));
    }

    private static byte[] ToBE(int v) =>
        new[] { (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v };

    private static uint Crc32(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
        }
        return crc ^ 0xFFFFFFFF;
    }
}