using Microsoft.AspNetCore.Mvc;
using Tracker.Api.Services.Interfaces;
using Tracker.Core.DTOs;

namespace Tracker.Api.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController(IProjectService projects, IScanService scan) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List()
        => Ok(await projects.ListAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var result = await projects.GetAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest req)
    {
        var result = await projects.CreateAsync(req.Name, req.RootPath, req.LanguageId);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPost("{id:int}/scan")]
    public async Task<IActionResult> TriggerScan(int id)
    {
        var job = await scan.TriggerScanAsync(id);
        return Accepted(job);
    }

    [HttpGet("{id:int}/scan/status")]
    public IActionResult ScanStatus(int id, [FromQuery] Guid jobId)
    {
        var status = scan.GetStatus(jobId);
        return status is null ? NotFound() : Ok(status);
    }
}
