using AuthX.Core.Constants;
using AuthX.Core.DTOs.Batches;
using AuthX.Core.DTOs.Common;
using AuthX.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthX.API.Controllers;

public class BatchesController : BaseController
{
    private readonly IBatchService _svc;
    public BatchesController(IBatchService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams p)
        => OkResult(await _svc.GetAllAsync(CurrentCompanyId, p));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
        => OkResult(await _svc.GetByIdAsync(CurrentCompanyId, id));

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager},{AppRoles.Production}")]
    public async Task<IActionResult> Create([FromBody] CreateBatchDto dto)
        => OkResult(
            await _svc.CreateAsync(CurrentCompanyId, CurrentUserId, dto),
            "Batch created.");

    [HttpPatch("{id:long}/status")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<IActionResult> UpdateStatus(long id, [FromQuery] string status)
    {
        await _svc.UpdateStatusAsync(CurrentCompanyId, id, status);
        return OkMessage("Batch status updated.");
    }

    [HttpGet("{id:long}/progress")]
    public async Task<IActionResult> Progress(long id)
        => OkResult(await _svc.GetBatchProgressAsync(id)); // via IQRService
}