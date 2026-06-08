using Microsoft.AspNetCore.Mvc;
using Tracker.Api.Services.Interfaces;

namespace Tracker.Api.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController(ISearchService svc) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] int? projectId,
        [FromQuery] int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(q)) return BadRequest("q is required");
        return Ok(await svc.SearchAsync(q, projectId, limit));
    }
}
