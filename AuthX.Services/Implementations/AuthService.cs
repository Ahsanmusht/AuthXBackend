// AuthX.Services/Implementations/AuthService.cs
// CLEAN FIX: isOwner flag MenuService ko pass karo
// Owner = saare menus, no permission check

using AuthX.Core.DTOs.Auth;
using AuthX.Core.Interfaces;
using AuthX.Services.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AuthX.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork    _uow;
    private readonly IJwtHelper     _jwt;
    private readonly IConfiguration _config;
    private readonly IMenuService   _menuService;

    public AuthService(
        IUnitOfWork uow,
        IJwtHelper jwt,
        IConfiguration config,
        IMenuService menuService)
    {
        _uow         = uow;
        _jwt         = jwt;
        _config      = config;
        _menuService = menuService;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var user = await _uow.Users.Query()
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u =>
                u.Email == dto.Email.Trim().ToLower() && u.IsActive);

        if (user == null || !PasswordHasher.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var roles = _uow.UserRoles.Query()
            .Where(ur => ur.UserId == user.UserId)
            .Select(ur => ur.Role.RoleName)
            .ToList();

        // Owner ke liye Admin role bhi add karo (API authorization ke liye)
        var effectiveRoles = roles.ToList();
        if (user.IsOwner && !effectiveRoles.Contains("Admin"))
            effectiveRoles.Add("Admin");

        // Owner ke liye selected company ka menu load karo (pehli active company)
        // Normal user ke liye apni company ka menu
        int menuCompanyId = user.CompanyId;
        if (user.IsOwner)
        {
            var firstCo = await _uow.Companies.Query()
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyId)
                .Select(c => new { c.CompanyId })
                .FirstOrDefaultAsync();
            menuCompanyId = firstCo?.CompanyId ?? user.CompanyId;
        }

        // KEY: isOwner = true hoga to MenuService permission check skip karega
        var menu = await _menuService.GetMenuForUserAsync(
            menuCompanyId,
            effectiveRoles,
            isOwner: user.IsOwner);

        var accessToken  = _jwt.GenerateAccessToken(user, effectiveRoles);
        var refreshToken = _jwt.GenerateRefreshToken();
        var expiry       = int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7");

        user.RefreshToken       = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(expiry);
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync();

        return new LoginResponseDto
        {
            AccessToken       = accessToken,
            RefreshToken      = refreshToken,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(
                int.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "60")),
            User = new UserInfoDto
            {
                UserId      = user.UserId,
                CompanyId   = user.CompanyId,
                Name        = user.Name,
                Email       = user.Email,
                Roles       = effectiveRoles,
                CompanyName = user.Company?.Name,
                CompanyLogo = user.Company?.LogoUrl,
                IsOwner     = user.IsOwner
            },
            Menu = menu
        };
    }

    public async Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        var principal = _jwt.GetPrincipalFromExpiredToken(dto.AccessToken)
            ?? throw new UnauthorizedAccessException("Invalid access token.");

        var userId = int.Parse(principal.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

        var user = await _uow.Users.GetByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("User not found.");

        if (user.RefreshToken != dto.RefreshToken ||
            user.RefreshTokenExpiry < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token expired or invalid.");

        var roles = _uow.UserRoles.Query()
            .Where(ur => ur.UserId == user.UserId)
            .Select(ur => ur.Role.RoleName)
            .ToList();

        var effectiveRoles = roles.ToList();
        if (user.IsOwner && !effectiveRoles.Contains("Admin"))
            effectiveRoles.Add("Admin");

        var newAccessToken  = _jwt.GenerateAccessToken(user, effectiveRoles);
        var newRefreshToken = _jwt.GenerateRefreshToken();
        var expiry          = int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7");

        user.RefreshToken       = newRefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(expiry);
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync();

        return new LoginResponseDto
        {
            AccessToken       = newAccessToken,
            RefreshToken      = newRefreshToken,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(
                int.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "60")),
            User = new UserInfoDto
            {
                UserId    = user.UserId,
                CompanyId = user.CompanyId,
                Name      = user.Name,
                Email     = user.Email,
                Roles     = effectiveRoles,
                IsOwner   = user.IsOwner
            }
        };
    }

    public async Task RevokeTokenAsync(int userId)
    {
        var user = await _uow.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        user.RefreshToken       = null;
        user.RefreshTokenExpiry = null;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync();
    }
}