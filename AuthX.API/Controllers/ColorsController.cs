using AuthX.Core.Constants;
using AuthX.Core.DTOs.Colors;
using AuthX.Core.Entities;
using AuthX.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthX.API.Controllers;

public class ColorsController : BaseController
{
    private readonly IUnitOfWork _uow;
    public ColorsController(IUnitOfWork uow) => _uow = uow;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var colors = await _uow.Colors.Query()
            .Where(c => c.CompanyId == CurrentCompanyId)
            .Select(c => new ColorDto
            {
                ColorId = c.ColorId,
                Name    = c.Name,
                HexCode = c.HexCode,
                IsActive = c.IsActive
            })
            .ToListAsync();
        return OkResult(colors);
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<IActionResult> Create([FromBody] CreateColorDto dto)
    {
        var color = new Color
        {
            CompanyId = CurrentCompanyId,
            Name      = dto.Name.Trim(),
            HexCode   = dto.HexCode.Trim()
        };
        await _uow.Colors.AddAsync(color);
        await _uow.SaveChangesAsync();
        return OkResult(new ColorDto
        {
            ColorId = color.ColorId,
            Name    = color.Name,
            HexCode = color.HexCode,
            IsActive = color.IsActive
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var color = await _uow.Colors.FindOneAsync(
            c => c.CompanyId == CurrentCompanyId && c.ColorId == id)
            ?? throw new KeyNotFoundException("Color not found.");
        _uow.Colors.Remove(color);
        await _uow.SaveChangesAsync();
        return OkMessage("Color deleted.");
    }
}