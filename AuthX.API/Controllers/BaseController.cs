using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthX.Core.Constants;
using System.Security.Claims;

namespace AuthX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public abstract class BaseController : ControllerBase
{
    // ─── Current User helpers ──────────────────────────────
    protected int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    protected int CurrentCompanyId
    {
        get
        {
            // Owner ke liye: agar header mein company override hai to woh use karo
            if (IsOwner && Request.Headers.TryGetValue("X-Owner-CompanyId", out var val))
                if (int.TryParse(val, out var overrideId) && overrideId > 0)
                    return overrideId;
            return int.Parse(User.FindFirstValue("CompanyId") ?? "0");
        }
    }


    protected string CurrentUserEmail =>
        User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    protected IEnumerable<string> CurrentUserRoles =>
        User.FindAll(ClaimTypes.Role).Select(c => c.Value);

    protected bool IsAdmin =>
        CurrentUserRoles.Contains(AppRoles.Admin);

    protected bool IsOwner =>
User.FindFirstValue("IsOwner") == "true";


    protected bool IsSupport =>
        CurrentUserRoles.Contains(AppRoles.Support) || IsAdmin;

    // ─── Standard response helpers ─────────────────────────
    protected IActionResult OkResult<T>(T data, string? message = null)
        => Ok(new { success = true, data, message });

    protected IActionResult OkMessage(string message)
        => Ok(new { success = true, message });

    protected IActionResult BadRequestResult(string message)
        => BadRequest(new { success = false, message });

    protected IActionResult NotFoundResult(string message)
        => NotFound(new { success = false, message });

    protected IActionResult ForbiddenResult(string message)
        => StatusCode(403, new { success = false, message });
}