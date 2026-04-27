using AuthX.Core.Constants;
using AuthX.Core.DTOs.Categories;
using AuthX.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthX.API.Controllers;

public class CategoriesController : BaseController
{
    private readonly ICategoryService _svc;
    public CategoriesController(ICategoryService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => OkResult(await _svc.GetAllAsync(CurrentCompanyId));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => OkResult(await _svc.GetByIdAsync(CurrentCompanyId, id));

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
        => OkResult(await _svc.CreateAsync(CurrentCompanyId, dto), "Category created.");

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto)
        => OkResult(await _svc.UpdateAsync(CurrentCompanyId, id, dto));

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> SetActive(int id, [FromQuery] bool active)
    {
        await _svc.SetActiveAsync(CurrentCompanyId, id, active);
        return OkMessage($"Category {(active ? "activated" : "deactivated")}.");
    }
}