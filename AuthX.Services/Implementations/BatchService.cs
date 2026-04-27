using AuthX.Core.DTOs.Batches;
using AuthX.Core.DTOs.Common;
using AuthX.Core.Entities;
using AuthX.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthX.Services.Implementations;

public class BatchService : IBatchService
{
    private readonly IUnitOfWork _uow;
    public BatchService(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<BatchListDto>> GetAllAsync(int companyId, PaginationParams p)
    {
        var query = _uow.Batches.Query()
            .Where(b => b.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(p.Search))
            query = query.Where(b => b.BatchNo.Contains(p.Search));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((p.Page - 1) * p.PageSize)
            .Take(p.PageSize)
            .Select(b => new BatchListDto
            {
                BatchId        = b.BatchId,
                BatchNo        = b.BatchNo,
                ProductName    = b.Product.Name,
                CategoryName   = b.Product.Category.Name,
                ProductionDate = b.ProductionDate,
                Quantity       = b.Quantity,
                Status         = b.Status,
                CreatedAt      = b.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<BatchListDto>
        {
            Items      = items,
            TotalCount = total,
            Page       = p.Page,
            PageSize   = p.PageSize
        };
    }

    public async Task<BatchDetailDto> GetByIdAsync(int companyId, long batchId)
    {
        var b = await _uow.Batches.Query()
            .Where(x => x.CompanyId == companyId && x.BatchId == batchId)
            .Select(x => new BatchDetailDto
            {
                BatchId        = x.BatchId,
                BatchNo        = x.BatchNo,
                ProductId      = x.ProductId,
                ProductName    = x.Product.Name,
                CategoryName   = x.Product.Category.Name,
                ProductionDate = x.ProductionDate,
                Quantity       = x.Quantity,
                Status         = x.Status,
                CreatedAt      = x.CreatedAt,
                GeneratedQty   = x.Items.Count(i => i.Status != null),
                PrintedQty     = x.Items.Count(i => i.PrintStatus == "Printed"),
                DispatchedQty  = x.Items.Count(i => i.Status == "Dispatched")
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Batch not found.");

        return b;
    }

    public async Task<BatchDetailDto> CreateAsync(int companyId, int createdBy, CreateBatchDto dto)
    {
        var exists = await _uow.Batches.ExistsAsync(b => b.BatchNo == dto.BatchNo.Trim());
        if (exists)
            throw new InvalidOperationException("BatchNo already exists.");

        var batch = new ProductionBatch
        {
            CompanyId      = companyId,
            ProductId      = dto.ProductId,
            BatchNo        = dto.BatchNo.Trim().ToUpper(),
            ProductionDate = dto.ProductionDate,
            Quantity       = dto.Quantity,
            CreatedBy      = createdBy
        };

        await _uow.Batches.AddAsync(batch);
        await _uow.SaveChangesAsync();
        return await GetByIdAsync(companyId, batch.BatchId);
    }

    public async Task UpdateStatusAsync(int companyId, long batchId, string status)
    {
        var batch = await _uow.Batches.FindOneAsync(b =>
            b.CompanyId == companyId && b.BatchId == batchId)
            ?? throw new KeyNotFoundException("Batch not found.");

        batch.Status = status;
        _uow.Batches.Update(batch);
        await _uow.SaveChangesAsync();
    }
}