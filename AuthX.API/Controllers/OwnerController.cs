using AuthX.Core.Entities;
using AuthX.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AuthX.API.Controllers;

public class OwnerController : BaseController
{
    private readonly IUnitOfWork _uow;
    public OwnerController(IUnitOfWork uow) => _uow = uow;

    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies()
    {
        if (!IsOwner) return ForbiddenResult("Owner only.");
        var companies = await _uow.Companies.Query()
        .Where(c => c.IsActive)
        .OrderBy(c => c.Name)
        .Select(c => new { c.CompanyId, c.Name, c.LogoUrl, c.IsOwnerCompany })
        .ToListAsync();
        return OkResult(companies);
    }
}
