using AuthX.Core.DTOs.ReturnReasons;
using AuthX.Core.Entities;
using AuthX.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthX.Services.Implementations;

public class ReturnReasonService : IReturnReasonService
{
    private readonly IUnitOfWork _uow;
    public ReturnReasonService(IUnitOfWork uow) => _uow = uow;

    public async Task<List<ReturnReasonDto>> GetAllAsync(int companyId)
        => await _uow.ReturnReasons.Query()
            .Where(r => r.CompanyId == companyId)
            .OrderBy(r => r.Name)
            .Select(r => new ReturnReasonDto
            {
                ReturnReasonId = r.ReturnReasonId,
                Name           = r.Name,
                Description    = r.Description,
                IsActive       = r.IsActive,
                CreatedAt      = r.CreatedAt
            })
            .ToListAsync();

    public async Task<ReturnReasonDto> GetByIdAsync(int companyId, int id)
        => await _uow.ReturnReasons.Query()
            .Where(r => r.CompanyId == companyId && r.ReturnReasonId == id)
            .Select(r => new ReturnReasonDto
            {
                ReturnReasonId = r.ReturnReasonId,
                Name           = r.Name,
                Description    = r.Description,
                IsActive       = r.IsActive,
                CreatedAt      = r.CreatedAt
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Return Reason not found.");

    public async Task<ReturnReasonDto> CreateAsync(int companyId, CreateReturnReasonDto dto)
    {
        var reason = new ReturnReason
        {
            CompanyId   = companyId,
            Name        = dto.Name.Trim(),
            Description = dto.Description
        };
        await _uow.ReturnReasons.AddAsync(reason);
        await _uow.SaveChangesAsync();
        return await GetByIdAsync(companyId, reason.ReturnReasonId);
    }

    public async Task<ReturnReasonDto> UpdateAsync(int companyId, int id, UpdateReturnReasonDto dto)
    {
        var reason = await _uow.ReturnReasons.FindOneAsync(r =>
            r.CompanyId == companyId && r.ReturnReasonId == id)
            ?? throw new KeyNotFoundException("Return Reason not found.");

        reason.Name        = dto.Name.Trim();
        reason.Description = dto.Description;
        _uow.ReturnReasons.Update(reason);
        await _uow.SaveChangesAsync();
        return await GetByIdAsync(companyId, id);
    }

    public async Task SetActiveAsync(int companyId, int id, bool active)
    {
        var reason = await _uow.ReturnReasons.FindOneAsync(r =>
            r.CompanyId == companyId && r.ReturnReasonId == id)
            ?? throw new KeyNotFoundException("Return Reason not found.");

        reason.IsActive = active;
        _uow.ReturnReasons.Update(reason);
        await _uow.SaveChangesAsync();
    }
}