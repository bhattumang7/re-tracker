using Microsoft.AspNetCore.Mvc;
using Tracker.Api.Services.Interfaces;
using Tracker.Core.Enums;

namespace Tracker.Api.Controllers;

[ApiController]
[Route("api/milestones")]
public class MilestonesController(IMilestoneService svc) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? projectId)
        => Ok(await svc.ListAsync(projectId));

    [HttpGet("tree")]
    public async Task<IActionResult> Tree([FromQuery] int? projectId)
        => Ok(await svc.GetTreeAsync(projectId));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var result = await svc.GetAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:int}/tree")]
    public async Task<IActionResult> Subtree(int id)
    {
        var result = await svc.GetSubtreeAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:int}/methods")]
    public async Task<IActionResult> Methods(int id,
        [FromQuery] MigrationStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
        => Ok(await svc.GetMethodsAsync(id, status, page, pageSize));

    [HttpGet("{id:int}/next")]
    public async Task<IActionResult> Next(int id)
    {
        var result = await svc.GetNextAsync(id);
        return result is null ? NoContent() : Ok(result);
    }

    [HttpGet("{id:int}/graph")]
    public async Task<IActionResult> Graph(int id)
        => Ok(await svc.GetGraphAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMilestoneRequest req)
    {
        var result = await svc.CreateAsync(req.Name, req.Description, req.ProjectId, req.ParentId, req.SortOrder);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPost("{id:int}/methods/{methodId:int}")]
    public async Task<IActionResult> AddMethod(int id, int methodId)
    {
        await svc.AddMethodAsync(id, methodId);
        return Ok();
    }

    // Define the milestone as a top-level function's dependency subtree
    // (its transitive callees), so /next walks that subtree leaf-first.
    [HttpPost("{id:int}/scope/{rootMethodId:int}")]
    public async Task<IActionResult> ScopeToRoot(int id, int rootMethodId)
        => Ok(new { milestoneId = id, rootMethodId, members = await svc.ScopeToRootAsync(id, rootMethodId) });
}

public record CreateMilestoneRequest(string Name, string? Description, int ProjectId, int? ParentId, int SortOrder = 0);
