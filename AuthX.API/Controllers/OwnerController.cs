// AuthX.API/Controllers/OwnerController.cs
// UPDATED: Full Company CRUD for Owner
// Owner sirf apni companies manage kar sakta hai

using AuthX.Core.DTOs.Companies;
using AuthX.Core.Entities;
using AuthX.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthX.API.Controllers;

public class OwnerController : BaseController
{
    private readonly IUnitOfWork _uow;
    public OwnerController(IUnitOfWork uow) => _uow = uow;

    // ─── GET: All companies ──────────────────────────────────────────────────
    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies()
    {
        if (!IsOwner) return ForbiddenResult("Owner only.");

        var companies = await _uow.Companies.Query()
            .OrderBy(c => c.IsOwnerCompany ? 0 : 1)  // Owner company pehle
            .ThenBy(c => c.Name)
            .Select(c => new CompanyListDto
            {
                CompanyId      = c.CompanyId,
                Name           = c.Name,
                Domain         = c.Domain,
                LogoUrl        = c.LogoUrl,
                IsActive       = c.IsActive,
                IsOwnerCompany = c.IsOwnerCompany,
                CreatedAt      = c.CreatedAt,
                UserCount      = c.Users.Count(u => u.IsActive)
            })
            .ToListAsync();

        return OkResult(companies);
    }

    // ─── GET: Single company ────────────────────────────────────────────────
    [HttpGet("companies/{companyId:int}")]
    public async Task<IActionResult> GetCompany(int companyId)
    {
        if (!IsOwner) return ForbiddenResult("Owner only.");

        var company = await _uow.Companies.Query()
            .Where(c => c.CompanyId == companyId)
            .Select(c => new CompanyListDto
            {
                CompanyId      = c.CompanyId,
                Name           = c.Name,
                Domain         = c.Domain,
                LogoUrl        = c.LogoUrl,
                IsActive       = c.IsActive,
                IsOwnerCompany = c.IsOwnerCompany,
                CreatedAt      = c.CreatedAt,
                UserCount      = c.Users.Count(u => u.IsActive)
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Company not found.");

        return OkResult(company);
    }

    // ─── POST: Create new company ───────────────────────────────────────────
    [HttpPost("companies")]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyDto dto)
    {
        if (!IsOwner) return ForbiddenResult("Owner only.");

        // Create company
        var company = new Company
        {
            Name           = dto.Name.Trim(),
            Domain         = dto.Domain?.Trim(),
            LogoUrl        = dto.LogoUrl?.Trim(),
            IsActive       = true,
            IsOwnerCompany = false  // Nai company owner company nahi hogi
        };

        await _uow.Companies.AddAsync(company);
        await _uow.SaveChangesAsync();

        // Default roles bhi create karo nai company ke liye
        var defaultRoles = new[] { "Admin", "Manager", "Production", "Warehouse", "Support", "Viewer" };
        foreach (var roleName in defaultRoles)
        {
            await _uow.Roles.AddAsync(new Role
            {
                CompanyId = company.CompanyId,
                RoleName  = roleName,
                IsActive  = true
            });
        }
        await _uow.SaveChangesAsync();

        return OkResult(new CompanyListDto
        {
            CompanyId      = company.CompanyId,
            Name           = company.Name,
            Domain         = company.Domain,
            LogoUrl        = company.LogoUrl,
            IsActive       = company.IsActive,
            IsOwnerCompany = company.IsOwnerCompany,
            CreatedAt      = company.CreatedAt,
            UserCount      = 0
        }, "Company created successfully.");
    }

    // ─── PUT: Update company ────────────────────────────────────────────────
    [HttpPut("companies/{companyId:int}")]
    public async Task<IActionResult> UpdateCompany(int companyId, [FromBody] UpdateCompanyDto dto)
    {
        if (!IsOwner) return ForbiddenResult("Owner only.");

        var company = await _uow.Companies.GetByIdAsync(companyId)
            ?? throw new KeyNotFoundException("Company not found.");

        company.Name    = dto.Name.Trim();
        company.Domain  = dto.Domain?.Trim();
        company.LogoUrl = dto.LogoUrl?.Trim();
        company.UpdatedAt = DateTime.UtcNow;

        _uow.Companies.Update(company);
        await _uow.SaveChangesAsync();

        return OkResult(new CompanyListDto
        {
            CompanyId      = company.CompanyId,
            Name           = company.Name,
            Domain         = company.Domain,
            LogoUrl        = company.LogoUrl,
            IsActive       = company.IsActive,
            IsOwnerCompany = company.IsOwnerCompany,
            CreatedAt      = company.CreatedAt,
        }, "Company updated.");
    }

    // ─── PATCH: Toggle company active status ────────────────────────────────
    [HttpPatch("companies/{companyId:int}/status")]
    public async Task<IActionResult> SetCompanyActive(int companyId, [FromQuery] bool active)
    {
        if (!IsOwner) return ForbiddenResult("Owner only.");

        var company = await _uow.Companies.GetByIdAsync(companyId)
            ?? throw new KeyNotFoundException("Company not found.");

        if (company.IsOwnerCompany)
            return BadRequestResult("Owner's own company cannot be deactivated.");

        company.IsActive  = active;
        company.UpdatedAt = DateTime.UtcNow;
        _uow.Companies.Update(company);
        await _uow.SaveChangesAsync();

        return OkMessage($"Company {(active ? "activated" : "deactivated")}.");
    }
}