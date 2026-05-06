using AuthX.Core.DTOs.Categories;
using AuthX.Core.Entities;
using AuthX.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthX.Services.Implementations;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _uow;
    public CategoryService(IUnitOfWork uow) => _uow = uow;

    public async Task<List<CategoryDto>> GetAllAsync(int companyId)
        => await _uow.Categories.Query()
            .Where(c => c.CompanyId == companyId)
            .OrderBy(c => c.ParentId ?? 0).ThenBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                CategoryId  = c.CategoryId,
                ParentId    = c.ParentId,
                Name        = c.Name,
                Description = c.Description,
                IsActive    = c.IsActive,
                CreatedAt   = c.CreatedAt,
                ParentName  = c.Parent != null ? c.Parent.Name : null
            })
            .ToListAsync();

    public async Task<CategoryDto> GetByIdAsync(int companyId, int categoryId)
        => await _uow.Categories.Query()
            .Where(c => c.CompanyId == companyId && c.CategoryId == categoryId)
            .Select(c => new CategoryDto
            {
                CategoryId  = c.CategoryId,
                Name        = c.Name,
                Description = c.Description,
                IsActive    = c.IsActive,
                CreatedAt   = c.CreatedAt
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Category not found.");

    public async Task<CategoryDto> CreateAsync(int companyId, CreateCategoryDto dto)
    {
        var cat = new Category
        {
            CompanyId   = companyId,
            ParentId    = dto.ParentId,
            Name        = dto.Name.Trim(),
            Description = dto.Description
        };
        await _uow.Categories.AddAsync(cat);
        await _uow.SaveChangesAsync();
        return await GetByIdAsync(companyId, cat.CategoryId);
    }

    public async Task<CategoryDto> UpdateAsync(int companyId, int categoryId, UpdateCategoryDto dto)
    {
        var cat = await _uow.Categories.FindOneAsync(c =>
            c.CompanyId == companyId && c.CategoryId == categoryId)
            ?? throw new KeyNotFoundException("Category not found.");
        cat.ParentId    = dto.ParentId;
        cat.Name        = dto.Name.Trim();
        cat.Description = dto.Description;
        _uow.Categories.Update(cat);
        await _uow.SaveChangesAsync();
        return await GetByIdAsync(companyId, categoryId);
    }

    public async Task SetActiveAsync(int companyId, int categoryId, bool active)
    {
        var cat = await _uow.Categories.FindOneAsync(c =>
            c.CompanyId == companyId && c.CategoryId == categoryId)
            ?? throw new KeyNotFoundException("Category not found.");

        cat.IsActive = active;
        _uow.Categories.Update(cat);
        await _uow.SaveChangesAsync();
    }
}