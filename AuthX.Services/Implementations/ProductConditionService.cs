using Microsoft.EntityFrameworkCore;
using AuthX.Core.DTOs.ProductConditions;
using AuthX.Core.Entities;
using AuthX.Core.Interfaces;

namespace AuthX.Services.Implementations;

public class ProductConditionService : IProductConditionService
{
    private readonly IUnitOfWork _uow;

    public ProductConditionService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<List<ProductConditionDto>> GetAllAsync(int companyId)
    {
        return await _uow.ProductConditions.Query()
            .Where(pc => pc.CompanyId == companyId)
            .OrderBy(pc => pc.Name)
            .Select(pc => new ProductConditionDto
            {
                ProductConditionId = pc.ProductConditionId,
                Name               = pc.Name,
                Description        = pc.Description,
                IsActive           = pc.IsActive,
                CreatedAt          = pc.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<ProductConditionDto> GetByIdAsync(int companyId, int id)
    {
        var condition = await _uow.ProductConditions.Query()
            .Where(pc => pc.CompanyId == companyId && pc.ProductConditionId == id)
            .Select(pc => new ProductConditionDto
            {
                ProductConditionId = pc.ProductConditionId,
                Name               = pc.Name,
                Description        = pc.Description,
                IsActive           = pc.IsActive,
                CreatedAt          = pc.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (condition == null)
            throw new KeyNotFoundException("Product Condition not found.");

        return condition;
    }

    public async Task<ProductConditionDto> CreateAsync(int companyId, CreateProductConditionDto dto)
    {
        var condition = new ProductCondition
        {
            CompanyId   = companyId,
            Name        = dto.Name.Trim(),
            Description = dto.Description
        };

        await _uow.ProductConditions.AddAsync(condition);
        await _uow.SaveChangesAsync();

        return await GetByIdAsync(companyId, condition.ProductConditionId);
    }

    public async Task<ProductConditionDto> UpdateAsync(int companyId, int id, UpdateProductConditionDto dto)
    {
        var condition = await _uow.ProductConditions.FindOneAsync(pc =>
            pc.CompanyId == companyId && pc.ProductConditionId == id);

        if (condition == null)
            throw new KeyNotFoundException("Product Condition not found.");

        condition.Name        = dto.Name.Trim();
        condition.Description = dto.Description;

        _uow.ProductConditions.Update(condition);
        await _uow.SaveChangesAsync();

        return await GetByIdAsync(companyId, id);
    }

    public async Task SetActiveAsync(int companyId, int id, bool active)
    {
        var condition = await _uow.ProductConditions.FindOneAsync(pc =>
            pc.CompanyId == companyId && pc.ProductConditionId == id);

        if (condition == null)
            throw new KeyNotFoundException("Product Condition not found.");

        condition.IsActive = active;
        _uow.ProductConditions.Update(condition);
        await _uow.SaveChangesAsync();
    }
}