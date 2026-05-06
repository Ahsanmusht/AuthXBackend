using AuthX.Core.Constants;
using AuthX.Core.DTOs.Menu;
using AuthX.Services.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
 
namespace AuthX.API.Controllers;
 
public class MenuController : BaseController
{
    private readonly IMenuService _svc;
    public MenuController(IMenuService svc) => _svc = svc;
 
    /// <summary>Get menu for currently logged-in user (role-filtered)</summary>
    [HttpGet("my-menu")]
    public async Task<IActionResult> GetMyMenu()
    {
        var menu = await _svc.GetMenuForUserAsync(CurrentCompanyId, CurrentUserRoles);
        return OkResult(menu);
    }
 
    /// <summary>Admin: Get ALL menu items with permissions</summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> GetAll()
        => OkResult(await _svc.GetAllMenuItemsAsync(CurrentCompanyId));
 
    [HttpGet("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> GetById(int id)
        => OkResult(await _svc.GetByIdAsync(CurrentCompanyId, id));
 
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateMenuItemDto dto)
        => OkResult(await _svc.CreateAsync(CurrentCompanyId, dto), "Menu item created.");
 
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMenuItemDto dto)
        => OkResult(await _svc.UpdateAsync(CurrentCompanyId, id, dto));
 
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        await _svc.DeleteAsync(CurrentCompanyId, id);
        return OkMessage("Menu item deleted.");
    }
 
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> SetActive(int id, [FromQuery] bool active)
    {
        await _svc.SetActiveAsync(CurrentCompanyId, id, active);
        return OkMessage($"Menu item {(active ? "activated" : "deactivated")}.");
    }
 
    [HttpPost("permissions")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> UpdatePermissions([FromBody] MenuPermissionUpdateDto dto)
    {
        await _svc.UpdatePermissionsAsync(CurrentCompanyId, dto);
        return OkMessage("Permissions updated.");
    }
}