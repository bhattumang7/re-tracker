using Microsoft.AspNetCore.Mvc;
using Tracker.Api.Services.Interfaces;

namespace Tracker.Api.Controllers;

[ApiController]
[Route("api/summary")]
public class SummaryController(ISummaryService svc) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? projectId)
        => Ok(await svc.GetSummaryAsync(projectId));
}
