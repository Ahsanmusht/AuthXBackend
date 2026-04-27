using AuthX.Core.Constants;
using AuthX.Core.DTOs.QR;
using AuthX.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthX.API.Controllers;

public class QRController : BaseController
{
    private readonly IQRService _svc;
    public QRController(IQRService svc) => _svc = svc;

    /// <summary>Generate QR codes for a batch (bulk — up to 500k)</summary>
    [HttpPost("generate")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager},{AppRoles.Production}")]
    public async Task<IActionResult> Generate([FromBody] GenerateQRDto dto)
        => OkResult(await _svc.GenerateAsync(CurrentCompanyId, CurrentUserId, dto));

    /// <summary>Create a print job for a batch</summary>
    [HttpPost("print-job")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager},{AppRoles.Warehouse}")]
    public async Task<IActionResult> CreatePrintJob([FromBody] CreatePrintJobDto dto)
        => OkResult(await _svc.CreatePrintJobAsync(CurrentCompanyId, CurrentUserId, dto));

    /// <summary>Get print job status</summary>
    [HttpGet("print-job/{id:long}")]
    public async Task<IActionResult> GetPrintJob(long id)
        => OkResult(await _svc.GetPrintJobAsync(CurrentCompanyId, id));

    /// <summary>Export QR codes as CSV (for label printers)</summary>
    [HttpGet("export/{batchId:long}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager},{AppRoles.Warehouse}")]
    public async Task<IActionResult> Export(long batchId, [FromQuery] string format = "CSV")
    {
        var bytes = await _svc.ExportQRsAsync(CurrentCompanyId, batchId, format);
        return File(bytes, "text/csv", $"batch-{batchId}-qrcodes.csv");
    }

    /// <summary>Get batch generation progress</summary>
    [HttpGet("progress/{batchId:long}")]
    public async Task<IActionResult> Progress(long batchId)
        => OkResult(await _svc.GetBatchProgressAsync(batchId));
}