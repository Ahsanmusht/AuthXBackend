using AuthX.Core.Constants;
using AuthX.Core.DTOs.Claims;
using AuthX.Core.DTOs.Common;
using AuthX.Core.Entities;
using AuthX.Core.Interfaces;
using AuthX.Infrastructure.Cache;
using Microsoft.EntityFrameworkCore;

namespace AuthX.Services.Implementations;

public class ClaimService : IClaimService
{
    private readonly IUnitOfWork           _uow;
    private readonly IRedisCacheService    _cache;
    private readonly INotificationService  _notif;

    public ClaimService(
        IUnitOfWork uow,
        IRedisCacheService cache,
        INotificationService notif)
    {
        _uow   = uow;
        _cache = cache;
        _notif = notif;
    }

    public async Task<ScanResultDto> ScanQRAsync(
        string qrCode, string? ip, string? deviceInfo,
        decimal? lat, decimal? lon)
    {
        // Log scan (fire and forget write — no FK needed for ScanLog)
        _ = Task.Run(async () =>
        {
            try
            {
                await _uow.ScanLogs.AddAsync(new ScanLog
                {
                    QRCode     = qrCode,
                    ScanType   = "CustomerScan",
                    ScanTime   = DateTime.UtcNow,
                    IPAddress  = ip,
                    DeviceInfo = deviceInfo,
                    Latitude   = lat,
                    Longitude  = lon
                });
                await _uow.SaveChangesAsync();
            }
            catch { /* swallow — scan log must not break QR check */ }
        });

        // Try cache first
        var cached = await _cache.GetAsync<ScanResultDto>(CacheKeys.QRItem(qrCode));
        if (cached != null) return cached;

        var item = await _uow.ProductItems.Query()
            .Include(i => i.Product).ThenInclude(p => p.Category)
            .Include(i => i.Batch)
            .Include(i => i.Claims)
            .FirstOrDefaultAsync(i => i.QRCode == qrCode);

        if (item == null)
            return new ScanResultDto
            {
                Status  = "NotFound",
                Message = "This QR code is not recognized. The product may be counterfeit.",
                CanClaim = false
            };

        if (!item.IsActive)
            return new ScanResultDto
            {
                Status  = "Fake",
                Message = "This QR code has been deactivated.",
                CanClaim = false
            };

        if (item.Status == ItemStatuses.Generated || item.Status == ItemStatuses.Printed)
            return new ScanResultDto
            {
                Status  = "Genuine",
                Message = "Genuine product — not yet dispatched to market.",
                CanClaim = false,
                ProductName  = item.Product.Name,
                CategoryName = item.Product.Category.Name,
                SerialNo     = item.SerialNo,
                BatchNo      = item.Batch.BatchNo
            };

        // Check active claim
        var activeClaim = item.Claims
            .Where(c => c.Status != ClaimStatuses.Delivered)
            .OrderByDescending(c => c.ClaimDate)
            .FirstOrDefault();

        if (activeClaim != null)
        {
            var result = new ScanResultDto
            {
                Status       = "AlreadyClaimed",
                Message      = "This product is currently being serviced by our support team.",
                CanClaim     = false,
                ProductName  = item.Product.Name,
                CategoryName = item.Product.Category.Name,
                SerialNo     = item.SerialNo,
                BatchNo      = item.Batch.BatchNo,
                WarrantyStart = item.WarrantyStartDate,
                WarrantyEnd   = item.WarrantyEndDate,
                ClaimStatus  = activeClaim.Status
            };
            // Short cache for in-process items
            await _cache.SetAsync(CacheKeys.QRItem(qrCode), result, TimeSpan.FromMinutes(2));
            return result;
        }

        var underWarranty = item.WarrantyEndDate.HasValue &&
                            item.WarrantyEndDate.Value > DateTime.UtcNow;

        var scanResult = new ScanResultDto
        {
            Status          = "Genuine",
            Message         = underWarranty
                ? "✓ Genuine product — under warranty."
                : "Genuine product — warranty expired.",
            CanClaim        = true,
            IsUnderWarranty = underWarranty,
            ProductName     = item.Product.Name,
            CategoryName    = item.Product.Category.Name,
            SerialNo        = item.SerialNo,
            BatchNo         = item.Batch.BatchNo,
            WarrantyStart   = item.WarrantyStartDate,
            WarrantyEnd     = item.WarrantyEndDate
        };

        await _cache.SetAsync(CacheKeys.QRItem(qrCode), scanResult, TimeSpan.FromMinutes(5));
        return scanResult;
    }

    public async Task<ClaimDto> SubmitClaimAsync(SubmitClaimDto dto)
    {
        var item = await _uow.ProductItems.Query()
            .Include(i => i.Claims)
            .FirstOrDefaultAsync(i => i.QRCode == dto.QRCode)
            ?? throw new KeyNotFoundException("QR Code not found.");

        // Check active claim
        var activeClaim = item.Claims
            .FirstOrDefault(c => c.Status != ClaimStatuses.Delivered);

        if (activeClaim != null)
            throw new InvalidOperationException("An active claim already exists for this product.");

        // Find or create customer by phone
        var customer = await _uow.Customers.FindOneAsync(c => c.Phone == dto.Phone.Trim());
        if (customer == null)
        {
            customer = new Customer
            {
                Name    = dto.Name.Trim(),
                Phone   = dto.Phone.Trim(),
                Address = dto.Address
            };
            await _uow.Customers.AddAsync(customer);
            await _uow.SaveChangesAsync();
        }

        var claim = new Claim
        {
            CompanyId  = item.CompanyId,
            ItemId     = item.ItemId,
            CustomerId = customer.CustomerId,
            Remarks    = dto.Remarks
        };

        await _uow.Claims.AddAsync(claim);
        item.Status = ItemStatuses.Claimed;
        _uow.ProductItems.Update(item);
        await _uow.SaveChangesAsync();

        // History entry
        await _uow.ClaimHistories.AddAsync(new ClaimStatusHistory
        {
            ClaimId   = claim.ClaimId,
            Status    = ClaimStatuses.Open,
            UpdatedBy = 0, // system
            Notes     = "Claim submitted by customer"
        });
        await _uow.SaveChangesAsync();

        // Invalidate QR cache
        await _cache.RemoveAsync(CacheKeys.QRItem(dto.QRCode));

        // Notify support team
        await _notif.PushAsync(
            companyId:    item.CompanyId,
            type:         NotificationTypes.NewClaim,
            referenceId:  claim.ClaimId,
            message:      $"New claim received for {item.SerialNo} from {customer.Name}",
            targetRoleId: null, // broadcast to Support role via SignalR
            actionUrl:    $"/claims/{claim.ClaimId}");

        return new ClaimDto
        {
            ClaimId = claim.ClaimId,
            Status  = claim.Status,
            Message = "Your claim has been submitted. Our support team will contact you shortly."
        };
    }

    public async Task<PagedResult<ClaimListDto>> GetClaimsAsync(
        int companyId, ClaimFilterDto filter, PaginationParams p)
    {
        var query = _uow.Claims.Query()
            .Where(c => c.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(c => c.Status == filter.Status);

        if (filter.From.HasValue)
            query = query.Where(c => c.ClaimDate >= filter.From);

        if (filter.To.HasValue)
            query = query.Where(c => c.ClaimDate <= filter.To);

        if (filter.AssignedTo.HasValue)
            query = query.Where(c => c.AssignedTo == filter.AssignedTo);

        if (!string.IsNullOrWhiteSpace(p.Search))
            query = query.Where(c =>
                c.Customer.Name.Contains(p.Search) ||
                c.Customer.Phone.Contains(p.Search) ||
                c.Item.SerialNo.Contains(p.Search));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.ClaimDate)
            .Skip((p.Page - 1) * p.PageSize)
            .Take(p.PageSize)
            .Select(c => new ClaimListDto
            {
                ClaimId      = c.ClaimId,
                CustomerName = c.Customer.Name,
                Phone        = c.Customer.Phone,
                SerialNo     = c.Item.SerialNo,
                ProductName  = c.Item.Product.Name,
                Status       = c.Status,
                ClaimDate    = c.ClaimDate,
                AssignedTo   = c.AssignedTo == null ? null :
                    _uow.Users.Query()
                        .Where(u => u.UserId == c.AssignedTo)
                        .Select(u => u.Name)
                        .FirstOrDefault()
            })
            .ToListAsync();

        return new PagedResult<ClaimListDto>
        {
            Items      = items,
            TotalCount = total,
            Page       = p.Page,
            PageSize   = p.PageSize
        };
    }

    public async Task<ClaimDetailDto> GetByIdAsync(int companyId, long claimId)
        => await _uow.Claims.Query()
            .Where(c => c.CompanyId == companyId && c.ClaimId == claimId)
            .Select(c => new ClaimDetailDto
            {
                ClaimId      = c.ClaimId,
                CustomerName = c.Customer.Name,
                Phone        = c.Customer.Phone,
                Address      = c.Customer.Address,
                SerialNo     = c.Item.SerialNo,
                QRCode       = c.Item.QRCode,
                ProductName  = c.Item.Product.Name,
                BatchNo      = c.Item.Batch.BatchNo,
                Status       = c.Status,
                Remarks      = c.Remarks,
                ClaimDate    = c.ClaimDate,
                WarrantyStart = c.Item.WarrantyStartDate,
                WarrantyEnd   = c.Item.WarrantyEndDate,
                History      = c.StatusHistory
                    .OrderBy(h => h.CreatedAt)
                    .Select(h => new ClaimHistoryDto
                    {
                        Status    = h.Status,
                        UpdatedBy = h.UpdatedBy.ToString(),
                        Notes     = h.Notes,
                        CreatedAt = h.CreatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Claim not found.");

    public async Task<ClaimDetailDto> UpdateStatusAsync(
        int companyId, long claimId, int updatedBy, UpdateClaimStatusDto dto)
    {
        var claim = await _uow.Claims.FindOneAsync(c =>
            c.CompanyId == companyId && c.ClaimId == claimId)
            ?? throw new KeyNotFoundException("Claim not found.");

        claim.LastStatus = claim.Status;
        claim.Status     = dto.Status;
        if (dto.AssignTo.HasValue)
            claim.AssignedTo = dto.AssignTo;

        _uow.Claims.Update(claim);

        await _uow.ClaimHistories.AddAsync(new ClaimStatusHistory
        {
            ClaimId   = claimId,
            Status    = dto.Status,
            UpdatedBy = updatedBy,
            Notes     = dto.Notes
        });

        await _uow.SaveChangesAsync();

        // Notify
        await _notif.PushAsync(
            companyId:   companyId,
            type:        NotificationTypes.ClaimUpdated,
            referenceId: claimId,
            message:     $"Claim #{claimId} updated to: {dto.Status}",
            actionUrl:   $"/claims/{claimId}");

        return await GetByIdAsync(companyId, claimId);
    }
}