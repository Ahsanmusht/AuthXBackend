using AuthX.Core.Constants;
using AuthX.Core.DTOs.Common;
using AuthX.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthX.API.Controllers;

public class DispatchController : BaseController
{
    private readonly IDispatchService _svc;
    private readonly IUnitOfWork _uow;
    public DispatchController(IDispatchService svc, IUnitOfWork uow)
    {
        _svc = svc;
        _uow = uow;
    }


    /// <summary>Scan QR to dispatch (activates warranty)</summary>
    [HttpPost("scan")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Warehouse}")]
    public async Task<IActionResult> Scan(
        [FromQuery] string qrCode,
        [FromQuery] string? location,
        [FromQuery] string? sapInvoiceNo)
        => OkResult(await _svc.ScanDispatchAsync(
            CurrentCompanyId, CurrentUserId, qrCode, location, sapInvoiceNo));

    /// <summary>Get dispatch history</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] long? batchId,
        [FromQuery] PaginationParams p)
        => OkResult(await _svc.GetDispatchesAsync(CurrentCompanyId, batchId, p));

    /// <summary>Scan by SerialNo to dispatch</summary>
    [HttpPost("scan-by-serial")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Warehouse}")]
    public async Task<IActionResult> ScanBySerial(
        [FromQuery] string serialNo,
        [FromQuery] string? location,
        [FromQuery] string? sapInvoiceNo)
    {
        // SerialNo se QR code dhundo
        var item = await _uow.ProductItems.Query()
            .Where(i => i.SerialNo == serialNo && i.CompanyId == CurrentCompanyId)
            .Select(i => new { i.QRCode })
            .FirstOrDefaultAsync();

        if (item == null)
            throw new KeyNotFoundException($"Serial No '{serialNo}' not found.");

        return OkResult(await _svc.ScanDispatchAsync(
            CurrentCompanyId, CurrentUserId, item.QRCode, location, sapInvoiceNo));
    }
}