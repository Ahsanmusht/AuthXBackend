using AuthX.Core.Constants;
using AuthX.Core.DTOs.Common;
using AuthX.Core.DTOs.Products;
using AuthX.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthX.API.Controllers;

public class ProductsController : BaseController
{
    private readonly IProductService _svc;
    public ProductsController(IProductService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams p)
        => OkResult(await _svc.GetAllAsync(CurrentCompanyId, p));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => OkResult(await _svc.GetByIdAsync(CurrentCompanyId, id));

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
        => OkResult(await _svc.CreateAsync(CurrentCompanyId, dto), "Product created.");

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
        => OkResult(await _svc.UpdateAsync(CurrentCompanyId, id, dto));

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> SetActive(int id, [FromQuery] bool active)
    {
        await _svc.SetActiveAsync(CurrentCompanyId, id, active);
        return OkMessage($"Product {(active ? "activated" : "deactivated")}.");
    }
}