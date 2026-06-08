using Microsoft.AspNetCore.Mvc;
using Tracker.Api.Services.Interfaces;
using Tracker.Core.DTOs;
using Tracker.Core.Enums;

namespace Tracker.Api.Controllers;

[ApiController]
[Route("api/methods")]
public class MethodsController(IMethodService svc) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] MigrationStatus? status,
        [FromQuery] int? fileId,
        [FromQuery] int? classId,
        [FromQuery] string? nameContains,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
        => Ok(await svc.ListAsync(status, fileId, classId, nameContains, page, pageSize));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var result = await svc.GetDetailAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest req)
    {
        var result = await svc.UpdateStatusAsync(id, req.Status, req.Comment);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:int}/rename")]
    public async Task<IActionResult> Rename(int id, [FromBody] RenameRequest req)
    {
        var result = await svc.RenameAsync(id, req.NewName, req.Comment);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:int}/port")]
    public async Task<IActionResult> SetPort(int id, [FromBody] SetPortRequest req)
    {
        var result = await svc.SetPortAsync(id, req.PortedName, req.PortedPath);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:int}/callers")]
    public async Task<IActionResult> Callers(int id)
        => Ok(await svc.GetCallersAsync(id));

    [HttpGet("{id:int}/callees")]
    public async Task<IActionResult> Callees(int id)
        => Ok(await svc.GetCalleesAsync(id));
}
