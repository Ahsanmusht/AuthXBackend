using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthX.Core.Constants;
using AuthX.Core.DTOs.ProductConditions;
using AuthX.Core.Interfaces;

namespace AuthX.API.Controllers;

public class ProductConditionsController : BaseController
{
    private readonly IProductConditionService _svc;

    public ProductConditionsController(IProductConditionService svc)
    {
        _svc = svc;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return OkResult(await _svc.GetAllAsync(CurrentCompanyId));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        return OkResult(await _svc.GetByIdAsync(CurrentCompanyId, id));
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<IActionResult> Create([FromBody] CreateProductConditionDto dto)
    {
        return OkResult(await _svc.CreateAsync(CurrentCompanyId, dto), "Product Condition created.");
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductConditionDto dto)
    {
        return OkResult(await _svc.UpdateAsync(CurrentCompanyId, id, dto));
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> SetActive(int id, [FromQuery] bool active)
    {
        await _svc.SetActiveAsync(CurrentCompanyId, id, active);
        return OkMessage($"Product Condition {(active ? "activated" : "deactivated")}.");
    }
}