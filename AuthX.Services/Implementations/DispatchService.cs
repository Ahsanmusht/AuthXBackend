using AuthX.Core.Constants;
using AuthX.Core.DTOs.Common;
using AuthX.Core.DTOs.Dispatch;
using AuthX.Core.Entities;
using AuthX.Core.Interfaces;
using AuthX.Infrastructure.Cache;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AuthX.Services.Implementations;

public class DispatchService : IDispatchService
{
    private readonly IUnitOfWork _uow;
    private readonly IRedisCacheService _cache;

    public DispatchService(IUnitOfWork uow, IRedisCacheService cache)
    {
        _uow = uow;
        _cache = cache;
    }

    // ─── Single QR Dispatch (existing — unchanged) ────────────────────────────
    public async Task<DispatchResultDto> ScanDispatchAsync(
        int companyId, int scannedBy, string qrCode, string? location, string? sapInvoiceNo)
    {
        var item = await _uow.ProductItems.Query()
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.QRCode == qrCode && i.CompanyId == companyId)
            ?? throw new KeyNotFoundException("QR Code not found.");

        if (item.Status == ItemStatuses.Dispatched)
            throw new InvalidOperationException("Item already dispatched.");

        if (item.Status != ItemStatuses.Printed && item.Status != ItemStatuses.Generated)
            throw new InvalidOperationException($"Item cannot be dispatched in status: {item.Status}");

        var settings = await _uow.CompanySettings.Query()
            .FirstOrDefaultAsync(s => s.CompanyId == companyId);

        DispatchItem(item, settings, scannedBy);

        await _uow.Dispatches.AddAsync(new Dispatch
        {
            CompanyId = companyId,
            ItemId = item.ItemId,
            ScannedBy = scannedBy,
            Location = location,
            DispatchDate = DateTime.UtcNow,
            SapInvoiceNo = sapInvoiceNo
        });

        await _uow.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.QRItem(qrCode));

        return new DispatchResultDto
        {
            DispatchId = 0,
            SerialNo = item.SerialNo,
            ProductName = item.Product.Name,
            DispatchDate = DateTime.UtcNow,
            WarrantyEnd = item.WarrantyEndDate!.Value
        };
    }

    // ─── BULK BATCH DISPATCH — Main new feature ───────────────────────────────
    // Multiple batches select karo, saare pending items ek saath dispatch ho jayein
    public async Task<BulkDispatchResultDto> BulkBatchDispatchAsync(
        int companyId, int scannedBy, BulkBatchDispatchDto dto)
    {
        if (!dto.BatchIds.Any())
            throw new ArgumentException("At least one batch must be selected.");

        // Company settings fetch karo
        var settings = await _uow.CompanySettings.Query()
            .FirstOrDefaultAsync(s => s.CompanyId == companyId);

        // Log entry create karo (tracking ke liye)
        var log = new BulkDispatchLog
        {
            CompanyId = companyId,
            DispatchedBy = scannedBy,
            BatchIds = JsonSerializer.Serialize(dto.BatchIds),
            Location = dto.Location,
            SapInvoiceNo = dto.SapInvoiceNo,
            StartedAt = DateTime.UtcNow,
            Status = "Processing"
        };
        await _uow.BulkDispatchLogs.AddAsync(log);
        await _uow.SaveChangesAsync();

        var batchSummaries = new List<BatchDispatchSummary>();
        int totalDispatched = 0;
        int totalSkipped = 0;

        // Har batch process karo
        foreach (var batchId in dto.BatchIds)
        {
            var batch = await _uow.Batches.Query()
                .Include(b => b.Product)
                .FirstOrDefaultAsync(b => b.BatchId == batchId && b.CompanyId == companyId);

            if (batch == null)
            {
                batchSummaries.Add(new BatchDispatchSummary
                {
                    BatchId = batchId,
                    BatchNo = $"Batch-{batchId}",
                    ProductName = "Not Found",
                    Dispatched = 0,
                    Skipped = 0,
                    Status = "BatchNotFound"
                });
                continue;
            }

            // Is batch ke saare dispatchable items fetch karo
            // Dispatchable = Generated ya Printed status
            var items = await _uow.ProductItems.Query()
                .Include(i => i.Product)
                .Where(i => i.BatchId == batchId
                         && i.CompanyId == companyId
                         && (i.Status == ItemStatuses.Generated || i.Status == ItemStatuses.Printed))
                .ToListAsync();

            int dispatched = 0;
            int skipped = 0;

            // Chunked processing — memory spike avoid karo for large batches
            // 10k items ek saath process karo
            const int chunkSize = 10_000;
            var now = DateTime.UtcNow;

            for (int offset = 0; offset < items.Count; offset += chunkSize)
            {
                var chunk = items.Skip(offset).Take(chunkSize).ToList();
                var dispatches = new List<Dispatch>();

                foreach (var item in chunk)
                {
                    DispatchItem(item, settings, scannedBy);
                    dispatches.Add(new Dispatch
                    {
                        CompanyId = companyId,
                        ItemId = item.ItemId,
                        ScannedBy = scannedBy,
                        Location = dto.Location,
                        DispatchDate = now,
                        SapInvoiceNo = dto.SapInvoiceNo
                    });
                    dispatched++;
                }

                await _uow.Dispatches.AddRangeAsync(dispatches);
                await _uow.SaveChangesAsync();

                // QR cache invalidate karo (bulk mein — prefix se)
                // Individual remove too slow for large batches; let TTL expire naturally
                // But remove batch progress cache
                await _cache.RemoveAsync(CacheKeys.BatchProgress(batchId.ToString()));
            }

            // Batch status update
            if (dispatched > 0)
            {
                batch.Status = BatchStatuses.Dispatched;
                _uow.Batches.Update(batch);
                await _uow.SaveChangesAsync();
            }

            // Count items jo already dispatched thay (skipped)
            skipped = await _uow.ProductItems.Query()
                .CountAsync(i => i.BatchId == batchId
                              && i.CompanyId == companyId
                              && i.Status == ItemStatuses.Dispatched)
                - dispatched; // Already dispatched minus jo abhi dispatch kiye

            // Skipped wo hain jo na Generated na Printed thay
            int nonDispatchable = await _uow.ProductItems.Query()
                .CountAsync(i => i.BatchId == batchId
                              && i.CompanyId == companyId
                              && i.Status != ItemStatuses.Generated
                              && i.Status != ItemStatuses.Printed
                              && i.Status != ItemStatuses.Dispatched);

            totalDispatched += dispatched;
            totalSkipped += nonDispatchable;

            batchSummaries.Add(new BatchDispatchSummary
            {
                BatchId = batchId,
                BatchNo = batch.BatchNo,
                ProductName = batch.Product.Name,
                Dispatched = dispatched,
                Skipped = nonDispatchable,
                Status = dispatched > 0 ? "Dispatched" : "AlreadyDispatched"
            });
        }

        // Log update karo
        log.TotalDispatched = totalDispatched;
        log.TotalSkipped = totalSkipped;
        log.CompletedAt = DateTime.UtcNow;
        log.Status = "Done";
        _uow.BulkDispatchLogs.Update(log);
        await _uow.SaveChangesAsync();

        return new BulkDispatchResultDto
        {
            LogId = log.LogId,
            TotalDispatched = totalDispatched,
            TotalSkipped = totalSkipped,
            Status = "Done",
            Batches = batchSummaries
        };
    }

    // ─── Helper: Single item dispatch logic (reusable) ────────────────────────
    private static void DispatchItem(ProductItem item, CompanySettings? settings, int scannedBy)
    {
        int delayDays = settings?.WarrantyDelayDays ?? 60;
        var now = DateTime.UtcNow;
        var warrantyStart = now.AddDays(delayDays);

        item.Status = ItemStatuses.Dispatched;
        item.WarrantyStartDate = warrantyStart;
        item.WarrantyEndDate = warrantyStart.AddDays(item.Product?.WarrantyDays ?? 365);
        item.IsFirstScan = true;
        item.FirstScanDate = now;
        item.FirstScanType = "DispatchScan";
        _setUpdated(item);
    }

    // EF tracking ke liye — Update call karna zaroori hai
    private static void _setUpdated(ProductItem item)
    {
        // EF Core tracks changes automatically when object is fetched via DbContext
        // No explicit update needed if using same DbContext instance
    }

    // ─── Get Dispatch History ─────────────────────────────────────────────────
    public async Task<PagedResult<DispatchListDto>> GetDispatchesAsync(
        int companyId, long? batchId, PaginationParams p)
    {
        var query = _uow.Dispatches.Query()
            .Where(d => d.CompanyId == companyId);

        if (batchId.HasValue)
            query = query.Where(d => d.Item.BatchId == batchId);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(d => d.DispatchDate)
            .Skip((p.Page - 1) * p.PageSize)
            .Take(p.PageSize)
            .Select(d => new DispatchListDto
            {
                DispatchId = d.DispatchId,
                SerialNo = d.Item.SerialNo,
                QRCode = d.Item.QRCode,
                ProductName = d.Item.Product.Name,
                Location = d.Location,
                DispatchDate = d.DispatchDate,
                ScannedBy = d.Item.Company.Name,
                WarrantyEnd = d.Item.WarrantyEndDate,
                SapInvoiceNo = d.SapInvoiceNo
            })
            .ToListAsync();

        return new PagedResult<DispatchListDto>
        {
            Items = items,
            TotalCount = total,
            Page = p.Page,
            PageSize = p.PageSize
        };
    }
}