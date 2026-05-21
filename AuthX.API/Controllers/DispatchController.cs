using AuthX.Core.Constants;
using AuthX.Core.DTOs.Common;
using AuthX.Core.DTOs.Dispatch;
using AuthX.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthX.API.Controllers;

public class DispatchController : BaseController
{
    private readonly IDispatchService _svc;
    private readonly IUnitOfWork _uow;

    public DispatchController(IDispatchService svc, IUnitOfWork uow)
    {
        _svc = svc;
        _uow = uow;
    }

    /// <summary>Single QR scan dispatch (existing)</summary>
    [HttpPost("scan")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Warehouse}")]
    public async Task<IActionResult> Scan(
        [FromQuery] string qrCode,
        [FromQuery] string? location,
        [FromQuery] string? sapInvoiceNo)
        => OkResult(await _svc.ScanDispatchAsync(
            CurrentCompanyId, CurrentUserId, qrCode, location, sapInvoiceNo));

    /// <summary>Scan by SerialNo (existing)</summary>
    [HttpPost("scan-by-serial")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Warehouse}")]
    public async Task<IActionResult> ScanBySerial(
        [FromQuery] string serialNo,
        [FromQuery] string? location,
        [FromQuery] string? sapInvoiceNo)
    {
        var item = await _uow.ProductItems.Query()
            .Where(i => i.SerialNo == serialNo && i.CompanyId == CurrentCompanyId)
            .Select(i => new { i.QRCode })
            .FirstOrDefaultAsync();

        if (item == null)
            throw new KeyNotFoundException($"Serial No '{serialNo}' not found.");

        return OkResult(await _svc.ScanDispatchAsync(
            CurrentCompanyId, CurrentUserId, item.QRCode, location, sapInvoiceNo));
    }

    /// <summary>Get dispatch history</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] long? batchId,
        [FromQuery] PaginationParams p)
        => OkResult(await _svc.GetDispatchesAsync(CurrentCompanyId, batchId, p));

    /// <summary>
    /// BULK BATCH DISPATCH — New Feature
    /// Multiple batches select karo, saare pending QR codes ek saath dispatch ho jayein.
    /// Admin ya Warehouse role required.
    /// POST /api/Dispatch/bulk-batch
    /// Body: { "batchIds": [1, 2, 3], "location": "Warehouse A", "sapInvoiceNo": "INV-001" }
    /// </summary>
    [HttpPost("bulk-batch")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Warehouse},{AppRoles.Manager}")]
    public async Task<IActionResult> BulkBatchDispatch([FromBody] BulkBatchDispatchDto dto)
    {
        try
        {
            if (dto.BatchIds == null || !dto.BatchIds.Any())
                return BadRequestResult("At least one batch ID is required.");

            if (dto.BatchIds.Count > 50)
                return BadRequestResult("Maximum 50 batches can be dispatched at once.");

            var result = await _svc.BulkBatchDispatchAsync(
                CurrentCompanyId, CurrentUserId, dto);

            return OkResult(result,
                $"Bulk dispatch complete — {result.TotalDispatched:N0} items dispatched across {result.Batches.Count} batches.");
        }
        catch (Exception ex)
        {
            // Log the actual error
            Console.WriteLine($"Error in BulkBatchDispatch: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");

            // Return detailed error in development
            return StatusCode(500, new
            {
                success = false,
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Get batches available for dispatch — filtered by status
    /// Returns batches with their pending item counts for the bulk dispatch UI
    /// GET /api/Dispatch/available-batches
    /// </summary>
    [HttpGet("available-batches")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Warehouse},{AppRoles.Manager}")]
    public async Task<IActionResult> GetAvailableBatches()
    {
        var batches = await _uow.Batches.Query()
            .Where(b => b.CompanyId == CurrentCompanyId
                     && (b.Status == BatchStatuses.QRGenerated || b.Status == BatchStatuses.Printed))
            .Select(b => new AvailableBatchDto
            {
                BatchId = b.BatchId,
                BatchNo = b.BatchNo,
                ProductName = b.Product.Name,
                CategoryName = b.Product.Category.Name,
                ProductionDate = b.ProductionDate,
                Status = b.Status,
                TotalItems = b.Items.Count(),
                PendingItems = b.Items.Count(i =>
                    i.Status == ItemStatuses.Generated || i.Status == ItemStatuses.Printed),
                DispatchedItems = b.Items.Count(i => i.Status == ItemStatuses.Dispatched),
                ColorName = b.Color != null ? b.Color.Name : null,
                ColorHexCode = b.Color != null ? b.Color.HexCode : null,
                ModelNo = b.Product.ModelNo,
                CreatedAt = b.CreatedAt
            })
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return OkResult(batches);
    }
}