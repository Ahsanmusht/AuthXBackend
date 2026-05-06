using AuthX.Core.DTOs.Promotion;
using AuthX.Core.Entities;
using AuthX.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
 
namespace AuthX.Services.Implementations;
 
public interface IPromotionService
{
    Task<List<PromotionDto>> GetAllAsync(int companyId);
    Task<PromotionDto>       GetByIdAsync(int companyId, int promotionId);
    Task<PromotionDto>       CreateAsync(int companyId, CreatePromotionDto dto);
    Task<PromotionDto>       UpdateAsync(int companyId, int promotionId, UpdatePromotionDto dto);
    Task                     DeleteAsync(int companyId, int promotionId);
    Task<PromotionDto>       SetActiveAsync(int companyId, int promotionId, bool active);
    Task<PromotionDto?>      GetActivePromotionAsync(int companyId);
}
 
public class PromotionService : IPromotionService
{
    private readonly IUnitOfWork _uow;
    public PromotionService(IUnitOfWork uow) => _uow = uow;
 
    public async Task<List<PromotionDto>> GetAllAsync(int companyId)
        => await _uow.Promotions.Query()
            .Where(p => p.CompanyId == companyId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => MapDto(p))
            .ToListAsync();
 
    public async Task<PromotionDto> GetByIdAsync(int companyId, int promotionId)
    {
        var p = await _uow.Promotions.FindOneAsync(x =>
            x.CompanyId == companyId && x.PromotionId == promotionId)
            ?? throw new KeyNotFoundException("Promotion not found.");
        return MapDto(p);
    }
 
    public async Task<PromotionDto> CreateAsync(int companyId, CreatePromotionDto dto)
    {
        var p = new PromotionSetup
        {
            CompanyId  = companyId,
            Title      = dto.Title?.Trim(),
            ImageUrl   = dto.ImageUrl.Trim(),
            ForwardUrl = dto.ForwardUrl?.Trim(),
            IsActive   = false  // New ones start inactive
        };
        await _uow.Promotions.AddAsync(p);
        await _uow.SaveChangesAsync();
        return MapDto(p);
    }
 
    public async Task<PromotionDto> UpdateAsync(int companyId, int promotionId, UpdatePromotionDto dto)
    {
        var p = await _uow.Promotions.FindOneAsync(x =>
            x.CompanyId == companyId && x.PromotionId == promotionId)
            ?? throw new KeyNotFoundException("Promotion not found.");
 
        p.Title      = dto.Title?.Trim();
        p.ImageUrl   = dto.ImageUrl.Trim();
        p.ForwardUrl = dto.ForwardUrl?.Trim();
        p.UpdatedAt  = DateTime.UtcNow;
 
        _uow.Promotions.Update(p);
        await _uow.SaveChangesAsync();
        return MapDto(p);
    }
 
    public async Task DeleteAsync(int companyId, int promotionId)
    {
        var p = await _uow.Promotions.FindOneAsync(x =>
            x.CompanyId == companyId && x.PromotionId == promotionId)
            ?? throw new KeyNotFoundException("Promotion not found.");
 
        _uow.Promotions.Remove(p);
        await _uow.SaveChangesAsync();
    }
 
    public async Task<PromotionDto> SetActiveAsync(int companyId, int promotionId, bool active)
    {
        // BUSINESS LOGIC: Only ONE can be active at a time
        if (active)
        {
            // Deactivate all others first
            var others = (await _uow.Promotions.FindAsync(p =>
                p.CompanyId == companyId && p.PromotionId != promotionId && p.IsActive)).ToList();
            foreach (var other in others)
            {
                other.IsActive = false;
                _uow.Promotions.Update(other);
            }
        }
 
        var p = await _uow.Promotions.FindOneAsync(x =>
            x.CompanyId == companyId && x.PromotionId == promotionId)
            ?? throw new KeyNotFoundException("Promotion not found.");
 
        p.IsActive  = active;
        p.UpdatedAt = DateTime.UtcNow;
        _uow.Promotions.Update(p);
        await _uow.SaveChangesAsync();
        return MapDto(p);
    }
 
    // Public endpoint - returns active promotion for customer page
    public async Task<PromotionDto?> GetActivePromotionAsync(int companyId)
    {
        var p = await _uow.Promotions.FindOneAsync(x =>
            x.CompanyId == companyId && x.IsActive);
        return p == null ? null : MapDto(p);
    }
 
    private static PromotionDto MapDto(PromotionSetup p) => new()
    {
        PromotionId = p.PromotionId,
        Title       = p.Title,
        ImageUrl    = p.ImageUrl,
        ForwardUrl  = p.ForwardUrl,
        IsActive    = p.IsActive,
        CreatedAt   = p.CreatedAt,
        UpdatedAt   = p.UpdatedAt
    };
}