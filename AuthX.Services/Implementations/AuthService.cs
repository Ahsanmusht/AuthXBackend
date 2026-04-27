using AuthX.Core.DTOs.Auth;
using AuthX.Core.Interfaces;
using AuthX.Services.Helpers;
using Microsoft.Extensions.Configuration;

namespace AuthX.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork     _uow;
    private readonly IJwtHelper      _jwt;
    private readonly IConfiguration  _config;

    public AuthService(IUnitOfWork uow, IJwtHelper jwt, IConfiguration config)
    {
        _uow    = uow;
        _jwt    = jwt;
        _config = config;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var user = await _uow.Users.FindOneAsync(u =>
            u.Email == dto.Email.Trim().ToLower() && u.IsActive);

        if (user == null || !PasswordHasher.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var roles = _uow.UserRoles.Query()
            .Where(ur => ur.UserId == user.UserId)
            .Select(ur => ur.Role.RoleName)
            .ToList();

        var accessToken  = _jwt.GenerateAccessToken(user, roles);
        var refreshToken = _jwt.GenerateRefreshToken();
        var expiry       = int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7");

        user.RefreshToken        = refreshToken;
        user.RefreshTokenExpiry  = DateTime.UtcNow.AddDays(expiry);
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
                UserId    = user.UserId,
                CompanyId = user.CompanyId,
                Name      = user.Name,
                Email     = user.Email,
                Roles     = roles
            }
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

        var newAccessToken  = _jwt.GenerateAccessToken(user, roles);
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
                Roles     = roles
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