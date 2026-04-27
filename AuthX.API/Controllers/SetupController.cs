using AuthX.Core.Entities;
using AuthX.Core.Interfaces;
using AuthX.Services.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SetupController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    public SetupController(IUnitOfWork uow) => _uow = uow;

    /// <summary>
    /// Sirf pehli baar — koi bhi user na ho tab kaam karta hai
    /// </summary>
    [AllowAnonymous]
    [HttpPost("first-admin")]
    public async Task<IActionResult> CreateFirstAdmin([FromBody] FirstAdminDto dto)
    {
        var anyUser = await _uow.Users.ExistsAsync(u => u.IsActive);
        if (anyUser)
            return BadRequest(new { success = false, message = "Already initialized." });

        var company = new Company
        {
            Name = dto.CompanyName, Domain = dto.Domain, IsActive = true
        };
        await _uow.Companies.AddAsync(company);
        await _uow.SaveChangesAsync();

        var roles = new[] { "Admin","Manager","Production","Warehouse","Support","Viewer" };
        Role? adminRole = null;
        foreach (var r in roles)
        {
            var role = new Role { CompanyId = company.CompanyId, RoleName = r, IsActive = true };
            await _uow.Roles.AddAsync(role);
            if (r == "Admin") adminRole = role;
        }
        await _uow.SaveChangesAsync();

        var user = new User
        {
            CompanyId    = company.CompanyId,
            Name         = dto.Name,
            Email        = dto.Email.Trim().ToLower(),
            Phone        = dto.Phone,
            PasswordHash = PasswordHasher.Hash(dto.Password),
            IsActive     = true
        };
        await _uow.Users.AddAsync(user);
        await _uow.SaveChangesAsync();

        await _uow.UserRoles.AddAsync(new UserRole
        {
            UserId = user.UserId,
            RoleId = adminRole!.RoleId
        });
        await _uow.SaveChangesAsync();

        return Ok(new { success = true, message = "Done! Now proceed to Login.", email = user.Email });
    }
}

public class FirstAdminDto
{
    public string CompanyName { get; set; } = null!;
    public string Domain      { get; set; } = null!;
    public string Name        { get; set; } = null!;
    public string Email       { get; set; } = null!;
    public string Phone       { get; set; } = null!;
    public string Password    { get; set; } = null!;
}