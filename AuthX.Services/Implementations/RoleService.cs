using AuthX.Core.DTOs.Users;
using AuthX.Core.Entities;
using AuthX.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthX.Services.Implementations;

public class RoleService : IRoleService
{
    private readonly IUnitOfWork _uow;
    public RoleService(IUnitOfWork uow) => _uow = uow;

    public async Task<List<RoleDto>> GetRolesAsync(int companyId)
        => await _uow.Roles.Query()
            .Where(r => r.CompanyId == companyId)
            .Select(r => new RoleDto
            {
                RoleId   = r.RoleId,
                RoleName = r.RoleName,
                IsActive = r.IsActive
            })
            .ToListAsync();

    public async Task<RoleDto> CreateAsync(int companyId, string roleName)
    {
        var role = new Role
        {
            CompanyId = companyId,
            RoleName  = roleName.Trim()
        };
        await _uow.Roles.AddAsync(role);
        await _uow.SaveChangesAsync();

        return new RoleDto
        {
            RoleId   = role.RoleId,
            RoleName = role.RoleName,
            IsActive = role.IsActive
        };
    }

    public async Task DeleteAsync(int companyId, int roleId)
    {
        var role = await _uow.Roles.FindOneAsync(r =>
            r.CompanyId == companyId && r.RoleId == roleId)
            ?? throw new KeyNotFoundException("Role not found.");

        _uow.Roles.Remove(role);
        await _uow.SaveChangesAsync();
    }
}