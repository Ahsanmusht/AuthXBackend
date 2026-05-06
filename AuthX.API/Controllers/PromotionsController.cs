using AuthX.Core.Constants;
using AuthX.Core.DTOs.Promotion;
using AuthX.Services.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
 
namespace AuthX.API.Controllers;
 
public class PromotionsController : BaseController
{
    private readonly IPromotionService _svc;
    public PromotionsController(IPromotionService svc) => _svc = svc;
 
    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<IActionResult> GetAll()
        => OkResult(await _svc.GetAllAsync(CurrentCompanyId));
 
    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<IActionResult> GetById(int id)
        => OkResult(await _svc.GetByIdAsync(CurrentCompanyId, id));
 
    /// <summary>Public: Get active promotion for customer page</summary>
    [AllowAnonymous]
    [HttpGet("active/{companyId:int}")]
    public async Task<IActionResult> GetActive(int companyId)
        => OkResult(await _svc.GetActivePromotionAsync(companyId));
 
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create([FromBody] CreatePromotionDto dto)
        => OkResult(await _svc.CreateAsync(CurrentCompanyId, dto), "Promotion created.");
 
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePromotionDto dto)
        => OkResult(await _svc.UpdateAsync(CurrentCompanyId, id, dto));
 
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        await _svc.DeleteAsync(CurrentCompanyId, id);
        return OkMessage("Promotion deleted.");
    }
 
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> SetActive(int id, [FromQuery] bool active)
        => OkResult(await _svc.SetActiveAsync(CurrentCompanyId, id, active));
}