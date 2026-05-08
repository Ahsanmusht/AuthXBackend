// AuthX.Services/Implementations/MenuService.cs
// KEY FIX: Owner ko saare menus milte hain - koi permission check nahi
// Normal users ke liye pehle jaisi hi permission-based filtering

using AuthX.Core.DTOs.Menu;
using AuthX.Core.Entities;
using AuthX.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthX.Services.Implementations;

public interface IMenuService
{
    Task<List<UserMenuDto>> GetMenuForUserAsync(int companyId, IEnumerable<string> userRoles, bool isOwner = false);
    Task<List<MenuItemDto>> GetAllMenuItemsAsync(int companyId);
    Task<MenuItemDto> GetByIdAsync(int companyId, int menuItemId);
    Task<MenuItemDto> CreateAsync(int companyId, CreateMenuItemDto dto);
    Task<MenuItemDto> UpdateAsync(int companyId, int menuItemId, UpdateMenuItemDto dto);
    Task DeleteAsync(int companyId, int menuItemId);
    Task UpdatePermissionsAsync(int companyId, MenuPermissionUpdateDto dto, int requestingUserId = 0, bool isOwner = true);
    Task SetActiveAsync(int companyId, int menuItemId, bool active);
    Task ReorderAsync(int companyId, List<(int id, int order)> items);
    Task<List<MenuItemDto>> GetAllowedMenuItemsAsync(int companyId, int userId);
}

public class MenuService : IMenuService
{
    private readonly IUnitOfWork _uow;
    public MenuService(IUnitOfWork uow) => _uow = uow;

    // ─── KEY FIX: Owner bypass ────────────────────────────────────────────────
    public async Task<List<UserMenuDto>> GetMenuForUserAsync(
        int companyId,
        IEnumerable<string> userRoles,
        bool isOwner = false)
    {
        // OWNER: koi permission check nahi — us company ke saare active menus
        if (isOwner)
        {
            var allItems = await _uow.MenuItems.Query()
                .Where(m => m.CompanyId == companyId && m.IsActive)
                .OrderBy(m => m.SortOrder)
                .ToListAsync();

            return BuildUserMenuTree(allItems, null);
        }

        // NORMAL USERS: pehle ki tarah permission-based filtering
        var userRolesList = userRoles.ToList();

        var allRoles = await _uow.Roles.Query()
            .Where(r => r.CompanyId == companyId && r.IsActive)
            .Select(r => new { r.RoleId, r.RoleName })
            .ToListAsync();

        var roleIds = allRoles
            .Where(r => userRolesList.Contains(r.RoleName))
            .Select(r => r.RoleId)
            .ToList();

        var allPermissions = await _uow.MenuPermissions.Query()
            .Where(mp => mp.CanView)
            .Select(mp => new { mp.RoleId, mp.MenuItemId })
            .ToListAsync();

        var allowedMenuItemIds = allPermissions
            .Where(mp => roleIds.Contains(mp.RoleId))
            .Select(mp => mp.MenuItemId)
            .Distinct()
            .ToList();

        var items = await _uow.MenuItems.Query()
            .Where(m => m.CompanyId == companyId && m.IsActive)
            .OrderBy(m => m.SortOrder)
            .ToListAsync();

        var allowedItems = FilterMenuTree(items, allowedMenuItemIds);
        return BuildUserMenuTree(allowedItems, null);
    }

    private static List<Core.Entities.MenuItem> FilterMenuTree(
        List<Core.Entities.MenuItem> all,
        List<int> allowedIds)
    {
        var result = new HashSet<int>();

        foreach (var id in allowedIds)
            result.Add(id);

        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var item in all)
            {
                if (item.ParentId.HasValue &&
                    result.Contains(item.MenuItemId) &&
                    !result.Contains(item.ParentId.Value))
                {
                    result.Add(item.ParentId.Value);
                    changed = true;
                }
            }
        }

        return all.Where(m => result.Contains(m.MenuItemId)).ToList();
    }

    private static List<UserMenuDto> BuildUserMenuTree(
        List<Core.Entities.MenuItem> items,
        int? parentId)
    {
        return items
            .Where(m => m.ParentId == parentId)
            .OrderBy(m => m.SortOrder)
            .Select(m => new UserMenuDto
            {
                MenuItemId = m.MenuItemId,
                ParentId = m.ParentId,
                Title = m.Title,
                Url = m.Url,
                Icon = m.Icon,
                Type = m.Type,
                SortOrder = m.SortOrder,
                Children = BuildUserMenuTree(items, m.MenuItemId)
            })
            .ToList();
    }

    public async Task<List<MenuItemDto>> GetAllMenuItemsAsync(int companyId)
    {
        var items = await _uow.MenuItems.Query()
            .Include(m => m.Permissions)
            .Where(m => m.CompanyId == companyId)
            .OrderBy(m => m.SortOrder)
            .ToListAsync();

        return BuildAdminMenuTree(items, null);
    }

    private static List<MenuItemDto> BuildAdminMenuTree(
        List<Core.Entities.MenuItem> items,
        int? parentId)
    {
        return items
            .Where(m => m.ParentId == parentId)
            .OrderBy(m => m.SortOrder)
            .Select(m => new MenuItemDto
            {
                MenuItemId = m.MenuItemId,
                ParentId = m.ParentId,
                Title = m.Title,
                Url = m.Url,
                Icon = m.Icon,
                Type = m.Type,
                SortOrder = m.SortOrder,
                IsActive = m.IsActive,
                AllowedRoleIds = m.Permissions.Select(p => p.RoleId).ToList(),
                Children = BuildAdminMenuTree(items, m.MenuItemId)
            })
            .ToList();
    }

    public async Task<MenuItemDto> GetByIdAsync(int companyId, int menuItemId)
    {
        var item = await _uow.MenuItems.Query()
            .Include(m => m.Permissions)
            .FirstOrDefaultAsync(m => m.CompanyId == companyId && m.MenuItemId == menuItemId)
            ?? throw new KeyNotFoundException("Menu item not found.");

        return new MenuItemDto
        {
            MenuItemId = item.MenuItemId,
            ParentId = item.ParentId,
            Title = item.Title,
            Url = item.Url,
            Icon = item.Icon,
            Type = item.Type,
            SortOrder = item.SortOrder,
            IsActive = item.IsActive,
            AllowedRoleIds = item.Permissions.Select(p => p.RoleId).ToList()
        };
    }

    public async Task<MenuItemDto> CreateAsync(int companyId, CreateMenuItemDto dto)
    {
        var item = new Core.Entities.MenuItem
        {
            CompanyId = companyId,
            ParentId = dto.ParentId,
            Title = dto.Title.Trim(),
            Url = dto.Url?.Trim(),
            Icon = dto.Icon?.Trim(),
            Type = dto.Type,
            SortOrder = dto.SortOrder
        };

        await _uow.MenuItems.AddAsync(item);
        await _uow.SaveChangesAsync();

        if (dto.RoleIds.Any())
        {
            var perms = dto.RoleIds.Select(rid => new MenuPermission
            {
                MenuItemId = item.MenuItemId,
                RoleId = rid,
                CanView = true
            });
            await _uow.MenuPermissions.AddRangeAsync(perms);
            await _uow.SaveChangesAsync();
        }

        return await GetByIdAsync(companyId, item.MenuItemId);
    }

    public async Task<MenuItemDto> UpdateAsync(int companyId, int menuItemId, UpdateMenuItemDto dto)
    {
        var item = await _uow.MenuItems.FindOneAsync(m =>
            m.CompanyId == companyId && m.MenuItemId == menuItemId)
            ?? throw new KeyNotFoundException("Menu item not found.");

        item.ParentId = dto.ParentId;
        item.Title = dto.Title.Trim();
        item.Url = dto.Url?.Trim();
        item.Icon = dto.Icon?.Trim();
        item.Type = dto.Type;
        item.SortOrder = dto.SortOrder;

        _uow.MenuItems.Update(item);

        var existingPerms = (await _uow.MenuPermissions
            .FindAsync(p => p.MenuItemId == menuItemId)).ToList();
        existingPerms.ForEach(p => _uow.MenuPermissions.Remove(p));

        if (dto.RoleIds.Any())
        {
            var newPerms = dto.RoleIds.Select(rid => new MenuPermission
            {
                MenuItemId = menuItemId,
                RoleId = rid,
                CanView = true
            });
            await _uow.MenuPermissions.AddRangeAsync(newPerms);
        }

        await _uow.SaveChangesAsync();
        return await GetByIdAsync(companyId, menuItemId);
    }

    public async Task DeleteAsync(int companyId, int menuItemId)
    {
        var item = await _uow.MenuItems.FindOneAsync(m =>
            m.CompanyId == companyId && m.MenuItemId == menuItemId)
            ?? throw new KeyNotFoundException("Menu item not found.");

        _uow.MenuItems.Remove(item);
        await _uow.SaveChangesAsync();
    }

    public async Task UpdatePermissionsAsync(
        int companyId,
        MenuPermissionUpdateDto dto,
        int requestingUserId = 0,
        bool isOwner = true)
    {
        if (!isOwner && requestingUserId > 0)
        {
            var myRoleIds = await _uow.UserRoles.Query()
                .Where(ur => ur.UserId == requestingUserId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            var myAllowedMenuIds = await _uow.MenuPermissions.Query()
                .Where(mp => myRoleIds.Contains(mp.RoleId) && mp.CanView)
                .Select(mp => mp.MenuItemId)
                .Distinct()
                .ToListAsync();

            if (!myAllowedMenuIds.Contains(dto.MenuItemId))
                throw new UnauthorizedAccessException(
                    "You can only assign permissions for menus you have access to.");
        }

        var item2 = await _uow.MenuItems.FindOneAsync(m =>
            m.CompanyId == companyId && m.MenuItemId == dto.MenuItemId)
            ?? throw new KeyNotFoundException("Menu item not found.");

        var existing = (await _uow.MenuPermissions
            .FindAsync(p => p.MenuItemId == dto.MenuItemId)).ToList();
        existing.ForEach(p => _uow.MenuPermissions.Remove(p));

        if (dto.RoleIds.Any())
        {
            var newPerms = dto.RoleIds.Select(rid => new MenuPermission
            {
                MenuItemId = dto.MenuItemId,
                RoleId = rid,
                CanView = true
            });
            await _uow.MenuPermissions.AddRangeAsync(newPerms);
        }

        await _uow.SaveChangesAsync();
    }

    public async Task SetActiveAsync(int companyId, int menuItemId, bool active)
    {
        var item = await _uow.MenuItems.FindOneAsync(m =>
            m.CompanyId == companyId && m.MenuItemId == menuItemId)
            ?? throw new KeyNotFoundException("Menu item not found.");

        item.IsActive = active;
        _uow.MenuItems.Update(item);
        await _uow.SaveChangesAsync();
    }

    public async Task ReorderAsync(int companyId, List<(int id, int order)> items)
    {
        foreach (var (id, order) in items)
        {
            var item = await _uow.MenuItems.FindOneAsync(m =>
                m.CompanyId == companyId && m.MenuItemId == id);
            if (item != null)
            {
                item.SortOrder = order;
                _uow.MenuItems.Update(item);
            }
        }
        await _uow.SaveChangesAsync();
    }

    public async Task<List<MenuItemDto>> GetAllowedMenuItemsAsync(int companyId, int userId)
    {
        var userRoleIds = await _uow.UserRoles.Query()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var allowedMenuIds = await _uow.MenuPermissions.Query()
            .Where(mp => userRoleIds.Contains(mp.RoleId) && mp.CanView)
            .Select(mp => mp.MenuItemId)
            .Distinct()
            .ToListAsync();

        var items = await _uow.MenuItems.Query()
            .Include(m => m.Permissions)
            .Where(m => m.CompanyId == companyId && allowedMenuIds.Contains(m.MenuItemId))
            .OrderBy(m => m.SortOrder)
            .ToListAsync();

        return BuildAdminMenuTree(items, null);
    }
}