using AuthX.Core.Constants;
using AuthX.Core.DTOs.Common;
using AuthX.Core.DTOs.Users;
using AuthX.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthX.API.Controllers;

public class UsersController : BaseController
{
    private readonly IUserService _users;
    private readonly IRoleService _roles;

    public UsersController(IUserService users, IRoleService roles)
    {
        _users = users;
        _roles = roles;
    }

    // ── Users ────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams p)
        => OkResult(await _users.GetUsersAsync(CurrentCompanyId, p));

    [HttpGet("{userId:int}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<IActionResult> GetById(int userId)
        => OkResult(await _users.GetByIdAsync(CurrentCompanyId, userId));

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        => OkResult(await _users.CreateAsync(CurrentCompanyId, dto), "User created.");

    [HttpPut("{userId:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Update(int userId, [FromBody] UpdateUserDto dto)
        => OkResult(await _users.UpdateAsync(CurrentCompanyId, userId, dto));

    [HttpPatch("{userId:int}/status")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> SetActive(int userId, [FromQuery] bool active)
    {
        await _users.SetActiveAsync(CurrentCompanyId, userId, active);
        return OkMessage($"User {(active ? "activated" : "deactivated")}.");
    }

    [HttpPut("{userId:int}/roles")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> AssignRoles(int userId, [FromBody] List<int> roleIds)
    {
        await _users.AssignRolesAsync(CurrentCompanyId, userId, roleIds);
        return OkMessage("Roles updated.");
    }

    // ── Roles ────────────────────────────────────────────────

    [HttpGet("/api/roles")]
    public async Task<IActionResult> GetRoles()
        => OkResult(await _roles.GetRolesAsync(CurrentCompanyId));

    [HttpPost("/api/roles")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> CreateRole([FromBody] string roleName)
        => OkResult(await _roles.CreateAsync(CurrentCompanyId, roleName), "Role created.");

    [HttpDelete("/api/roles/{roleId:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteRole(int roleId)
    {
        await _roles.DeleteAsync(CurrentCompanyId, roleId);
        return OkMessage("Role deleted.");
    }
}