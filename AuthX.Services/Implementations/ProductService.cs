using AuthX.Core.DTOs.Colors;
using AuthX.Core.DTOs.Common;
using AuthX.Core.DTOs.Products;
using AuthX.Core.Entities;
using AuthX.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthX.Services.Implementations;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;
    public ProductService(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<ProductListDto>> GetAllAsync(int companyId, PaginationParams p)
    {
        var query = _uow.Products.Query()
            .Where(x => x.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(p.Search))
            query = query.Where(x =>
                x.Name.Contains(p.Search) || x.SKU.Contains(p.Search));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((p.Page - 1) * p.PageSize)
            .Take(p.PageSize)
            .Select(x => new ProductListDto
            {
                ProductId = x.ProductId,
                Name = x.Name,
                SKU = x.SKU,
                CategoryName = x.Category.Name,
                CategoryId = x.CategoryId,
                WarrantyDays = x.WarrantyDays,
                Description = x.Description,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                ModelNo = x.ModelNo,
                ImageUrl = x.ImageUrl,
                Colors = x.ProductColors.Select(pc => new ColorDto
                {
                    ColorId = pc.Color.ColorId,
                    Name = pc.Color.Name,
                    HexCode = pc.Color.HexCode
                }).ToList()
            })
            .ToListAsync();

        return new PagedResult<ProductListDto>
        {
            Items = items,
            TotalCount = total,
            Page = p.Page,
            PageSize = p.PageSize
        };
    }

    public async Task<ProductDetailDto> GetByIdAsync(int companyId, int productId)
        => await _uow.Products.Query()
            .Where(x => x.CompanyId == companyId && x.ProductId == productId)
            .Select(x => new ProductDetailDto
            {
                ProductId = x.ProductId,
                Name = x.Name,
                SKU = x.SKU,
                CategoryId = x.CategoryId,
                CategoryName = x.Category.Name,
                WarrantyDays = x.WarrantyDays,
                Description = x.Description,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                Colors = x.ProductColors.Select(pc => new ColorDto
                {
                    ColorId = pc.Color.ColorId,
                    Name = pc.Color.Name,
                    HexCode = pc.Color.HexCode
                }).ToList()
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Product not found.");

    public async Task<ProductDetailDto> CreateAsync(int companyId, CreateProductDto dto)
    {
        var skuExists = await _uow.Products.ExistsAsync(p =>
            p.CompanyId == companyId && p.SKU == dto.SKU.Trim().ToUpper());

        if (skuExists)
            throw new InvalidOperationException("SKU already exists for this company.");

        var product = new Product
        {
            CompanyId = companyId,
            CategoryId = dto.CategoryId,
            Name = dto.Name.Trim(),
            SKU = dto.SKU.Trim().ToUpper(),
            WarrantyDays = dto.WarrantyDays,
            Description = dto.Description,
            ModelNo = dto.ModelNo,
            ImageUrl = dto.ImageUrl
        };

        await _uow.Products.AddAsync(product);
        await _uow.SaveChangesAsync();
        if (dto.ColorIds.Any())
        {
            var pColors = dto.ColorIds.Select(cid => new ProductColor
            {
                ProductId = product.ProductId,
                ColorId = cid
            });
            await _uow.ProductColors.AddRangeAsync(pColors);
            await _uow.SaveChangesAsync();
        }
        return await GetByIdAsync(companyId, product.ProductId);
    }

    public async Task<ProductDetailDto> UpdateAsync(int companyId, int productId, UpdateProductDto dto)
    {
        var product = await _uow.Products.FindOneAsync(p =>
            p.CompanyId == companyId && p.ProductId == productId)
            ?? throw new KeyNotFoundException("Product not found.");

        product.CategoryId = dto.CategoryId;
        product.Name = dto.Name.Trim();
        product.WarrantyDays = dto.WarrantyDays;
        product.Description = dto.Description;
        product.ModelNo = dto.ModelNo;
        if (!string.IsNullOrEmpty(dto.ImageUrl))
            product.ImageUrl = dto.ImageUrl;
        _uow.Products.Update(product);


        var existingColors = (await _uow.ProductColors
        .FindAsync(pc => pc.ProductId == productId)).ToList();
        existingColors.ForEach(pc => _uow.ProductColors.Remove(pc));

        if (dto.ColorIds.Any())
        {
            var newColors = dto.ColorIds.Select(cid => new ProductColor
            {
                ProductId = productId,
                ColorId = cid
            });
            await _uow.ProductColors.AddRangeAsync(newColors);
        }

        await _uow.SaveChangesAsync();
        return await GetByIdAsync(companyId, productId);
    }

    public async Task SetActiveAsync(int companyId, int productId, bool active)
    {
        var product = await _uow.Products.FindOneAsync(p =>
            p.CompanyId == companyId && p.ProductId == productId)
            ?? throw new KeyNotFoundException("Product not found.");

        product.IsActive = active;
        _uow.Products.Update(product);
        await _uow.SaveChangesAsync();
    }
}