using AuthX.Core.DTOs.Common;
using AuthX.Core.DTOs.Users;
using AuthX.Core.Entities;
using AuthX.Core.Interfaces;
using AuthX.Services.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AuthX.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUnitOfWork _uow;
    public UserService(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<UserListDto>> GetUsersAsync(int companyId, PaginationParams p)
    {
        var query = _uow.Users.Query()
            .Where(u => u.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(p.Search))
            query = query.Where(u =>
                u.Name.Contains(p.Search) || u.Email.Contains(p.Search));

        var total = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((p.Page - 1) * p.PageSize)
            .Take(p.PageSize)
            .Select(u => new UserListDto
            {
                UserId    = u.UserId,
                Name      = u.Name,
                Email     = u.Email,
                Phone     = u.Phone,
                IsActive  = u.IsActive,
                CreatedAt = u.CreatedAt,
                Roles     = u.UserRoles.Select(ur => ur.Role.RoleName).ToList()
            })
            .ToListAsync();

        return new PagedResult<UserListDto>
        {
            Items      = users,
            TotalCount = total,
            Page       = p.Page,
            PageSize   = p.PageSize
        };
    }

    public async Task<UserDetailDto> GetByIdAsync(int companyId, int userId)
    {
        var u = await _uow.Users.Query()
            .Where(x => x.CompanyId == companyId && x.UserId == userId)
            .Select(u => new UserDetailDto
            {
                UserId    = u.UserId,
                Name      = u.Name,
                Email     = u.Email,
                Phone     = u.Phone,
                IsActive  = u.IsActive,
                CreatedAt = u.CreatedAt,
                Roles     = u.UserRoles.Select(ur => ur.Role.RoleName).ToList()
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("User not found.");

        return u;
    }

    public async Task<UserDetailDto> CreateAsync(int companyId, CreateUserDto dto)
    {
        var emailExists = await _uow.Users.ExistsAsync(u =>
            u.Email == dto.Email.Trim().ToLower());

        if (emailExists)
            throw new InvalidOperationException("Email already in use.");

        var user = new User
        {
            CompanyId    = companyId,
            Name         = dto.Name.Trim(),
            Email        = dto.Email.Trim().ToLower(),
            Phone        = dto.Phone,
            PasswordHash = PasswordHasher.Hash(dto.Password)
        };

        await _uow.Users.AddAsync(user);
        await _uow.SaveChangesAsync();

        if (dto.RoleIds.Any())
        {
            var userRoles = dto.RoleIds.Select(rid => new UserRole
            {
                UserId = user.UserId,
                RoleId = rid
            });
            await _uow.UserRoles.AddRangeAsync(userRoles);
            await _uow.SaveChangesAsync();
        }

        return await GetByIdAsync(companyId, user.UserId);
    }

    public async Task<UserDetailDto> UpdateAsync(int companyId, int userId, UpdateUserDto dto)
    {
        var user = await _uow.Users.FindOneAsync(u =>
            u.CompanyId == companyId && u.UserId == userId)
            ?? throw new KeyNotFoundException("User not found.");

        user.Name  = dto.Name.Trim();
        user.Phone = dto.Phone;
        _uow.Users.Update(user);

        // Sync roles
        var existing = (await _uow.UserRoles.FindAsync(ur => ur.UserId == userId)).ToList();
        existing.ForEach(ur => _uow.UserRoles.Remove(ur));

        if (dto.RoleIds.Any())
        {
            var newRoles = dto.RoleIds.Select(rid => new UserRole
            {
                UserId = userId,
                RoleId = rid
            });
            await _uow.UserRoles.AddRangeAsync(newRoles);
        }

        await _uow.SaveChangesAsync();
        return await GetByIdAsync(companyId, userId);
    }

    public async Task SetActiveAsync(int companyId, int userId, bool active)
    {
        var user = await _uow.Users.FindOneAsync(u =>
            u.CompanyId == companyId && u.UserId == userId)
            ?? throw new KeyNotFoundException("User not found.");

        user.IsActive = active;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync();
    }

    public async Task AssignRolesAsync(int companyId, int userId, List<int> roleIds)
    {
        var user = await _uow.Users.FindOneAsync(u =>
            u.CompanyId == companyId && u.UserId == userId)
            ?? throw new KeyNotFoundException("User not found.");

        var existing = (await _uow.UserRoles.FindAsync(ur => ur.UserId == userId)).ToList();
        existing.ForEach(ur => _uow.UserRoles.Remove(ur));

        var newRoles = roleIds.Select(rid => new UserRole
        {
            UserId = userId,
            RoleId = rid
        });
        await _uow.UserRoles.AddRangeAsync(newRoles);
        await _uow.SaveChangesAsync();
    }
}