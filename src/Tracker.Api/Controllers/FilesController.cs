using Microsoft.AspNetCore.Mvc;
using Tracker.Api.Services.Interfaces;

namespace Tracker.Api.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController(IFileService svc) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? projectId)
        => Ok(await svc.ListAsync(projectId));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var result = await svc.GetAsync(id);
        return result is null ? NotFound() : Ok(result);
    }
}
