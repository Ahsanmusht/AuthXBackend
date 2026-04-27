using AuthX.Core.Constants;
using AuthX.Core.DTOs.Common;
using AuthX.Core.DTOs.Dispatch;
using AuthX.Core.Entities;
using AuthX.Core.Interfaces;
using AuthX.Infrastructure.Cache;
using Microsoft.EntityFrameworkCore;

namespace AuthX.Services.Implementations;

public class DispatchService : IDispatchService
{
    private readonly IUnitOfWork        _uow;
    private readonly IRedisCacheService _cache;

    public DispatchService(IUnitOfWork uow, IRedisCacheService cache)
    {
        _uow   = uow;
        _cache = cache;
    }

    public async Task<DispatchResultDto> ScanDispatchAsync(
        int companyId, int scannedBy, string qrCode, string? location)
    {
        var item = await _uow.ProductItems.Query()
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i =>
                i.QRCode == qrCode && i.CompanyId == companyId)
            ?? throw new KeyNotFoundException("QR Code not found.");

        if (item.Status == ItemStatuses.Dispatched)
            throw new InvalidOperationException("Item already dispatched.");

        if (item.Status != ItemStatuses.Printed &&
            item.Status != ItemStatuses.Generated)
            throw new InvalidOperationException($"Item cannot be dispatched in status: {item.Status}");

        var now = DateTime.UtcNow;

        // Set warranty
        item.Status            = ItemStatuses.Dispatched;
        item.WarrantyStartDate = now;
        item.WarrantyEndDate   = now.AddDays(item.Product.WarrantyDays);
        item.IsFirstScan       = true;
        item.FirstScanDate     = now;
        item.FirstScanType     = "DispatchScan";

        _uow.ProductItems.Update(item);

        await _uow.Dispatches.AddAsync(new Dispatch
        {
            CompanyId  = companyId,
            ItemId     = item.ItemId,
            ScannedBy  = scannedBy,
            Location   = location,
            DispatchDate = now
        });

        await _uow.SaveChangesAsync();

        // Invalidate QR cache
        await _cache.RemoveAsync(CacheKeys.QRItem(qrCode));

        return new DispatchResultDto
        {
            DispatchId   = 0,
            SerialNo     = item.SerialNo,
            ProductName  = item.Product.Name,
            DispatchDate = now,
            WarrantyEnd  = item.WarrantyEndDate!.Value
        };
    }

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
                DispatchId   = d.DispatchId,
                SerialNo     = d.Item.SerialNo,
                QRCode       = d.Item.QRCode,
                ProductName  = d.Item.Product.Name,
                Location     = d.Location,
                DispatchDate = d.DispatchDate,
                ScannedBy    = d.Item.Company.Name
            })
            .ToListAsync();

        return new PagedResult<DispatchListDto>
        {
            Items      = items,
            TotalCount = total,
            Page       = p.Page,
            PageSize   = p.PageSize
        };
    }
}