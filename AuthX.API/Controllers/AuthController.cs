using AuthX.Core.DTOs.Auth;
using AuthX.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthX.API.Controllers;

public class AuthController : BaseController
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Login and get JWT tokens</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var result = await _auth.LoginAsync(dto);
        return OkResult(result);
    }

    /// <summary>Refresh expired access token</summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
    {
        var result = await _auth.RefreshTokenAsync(dto);
        return OkResult(result);
    }

    /// <summary>Revoke refresh token (logout)</summary>
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke()
    {
        await _auth.RevokeTokenAsync(CurrentUserId);
        return OkMessage("Logged out successfully.");
    }

    /// <summary>Get current user info</summary>
    [HttpGet("me")]
    public IActionResult Me() => OkResult(new
    {
        UserId    = CurrentUserId,
        CompanyId = CurrentCompanyId,
        Email     = CurrentUserEmail,
        Roles     = CurrentUserRoles
    });
}