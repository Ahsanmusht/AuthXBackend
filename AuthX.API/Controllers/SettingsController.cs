using AuthX.Core.DTOs.Settings;
using AuthX.Core.Entities;
using AuthX.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthX.API.Controllers;

public class SettingsController : BaseController
{
    private readonly IUnitOfWork _uow;
    public SettingsController(IUnitOfWork uow) => _uow = uow;

    [HttpGet("print")]
    public async Task<IActionResult> GetPrintSettings()
    {
        var s = await _uow.PrintSettings.Query()
            .FirstOrDefaultAsync(x => x.CompanyId == CurrentCompanyId);

        if (s == null)
            return OkResult(new PrintSettingsDto());

        return OkResult(new PrintSettingsDto
        {
            LabelWidthMm = s.LabelWidthMm,
            LabelHeightMm = s.LabelHeightMm,
            QRSizeMm = s.QRSizeMm,
            ColumnsPerRow = s.ColumnsPerRow,
            ShowProductName = s.ShowProductName,
            ShowSerialNo = s.ShowSerialNo,
            ShowBatchNo = s.ShowBatchNo,
            ShowColorName = s.ShowColorName,
            ShowModelNo = s.ShowModelNo,
            ShowCompanyName = s.ShowCompanyName,
            WarrantyDelayDays = s.WarrantyDelayDays
        });
    }

    [HttpPost("print")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SavePrintSettings([FromBody] PrintSettingsDto dto)
    {
        var s = await _uow.PrintSettings.Query()
            .FirstOrDefaultAsync(x => x.CompanyId == CurrentCompanyId);

        if (s == null)
        {
            s = new PrintSettings { CompanyId = CurrentCompanyId };
            await _uow.PrintSettings.AddAsync(s);
        }

        s.LabelWidthMm = dto.LabelWidthMm;
        s.LabelHeightMm = dto.LabelHeightMm;
        s.QRSizeMm = dto.QRSizeMm;
        s.ColumnsPerRow = dto.ColumnsPerRow;
        s.ShowProductName = dto.ShowProductName;
        s.ShowSerialNo = dto.ShowSerialNo;
        s.ShowBatchNo = dto.ShowBatchNo;
        s.ShowColorName = dto.ShowColorName;
        s.ShowModelNo = dto.ShowModelNo;
        s.ShowCompanyName = dto.ShowCompanyName;
        s.WarrantyDelayDays = dto.WarrantyDelayDays;
        s.UpdatedAt = DateTime.UtcNow;

        if (s.Id > 0) _uow.PrintSettings.Update(s);
        await _uow.SaveChangesAsync();
        return OkMessage("Print settings saved.");
    }
    [HttpGet("company")]
    public async Task<IActionResult> GetCompanySettings()
    {
        var s = await _uow.CompanySettings.Query()
            .FirstOrDefaultAsync(x => x.CompanyId == CurrentCompanyId);

        if (s == null)
            return OkResult(new CompanySettingsDto());

        return OkResult(new CompanySettingsDto
        {
            ColorMode = s.ColorMode ?? "Multi",
            WarrantyDelayDays = s.WarrantyDelayDays
        });
    }

    [HttpPost("company")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SaveCompanySettings([FromBody] CompanySettingsDto dto)
    {
        var s = await _uow.CompanySettings.Query()
            .FirstOrDefaultAsync(x => x.CompanyId == CurrentCompanyId);

        if (s == null)
        {
            s = new CompanySettings { CompanyId = CurrentCompanyId };
            await _uow.CompanySettings.AddAsync(s);
        }

        s.ColorMode = dto.ColorMode;
        s.WarrantyDelayDays = dto.WarrantyDelayDays;
        s.UpdatedAt = DateTime.UtcNow;

        if (s.Id > 0) _uow.CompanySettings.Update(s);
        await _uow.SaveChangesAsync();
        return OkMessage("Company settings saved.");
    }
}