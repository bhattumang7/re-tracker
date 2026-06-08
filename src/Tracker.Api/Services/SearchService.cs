using Microsoft.EntityFrameworkCore;
using Tracker.Api.Services.Interfaces;
using Tracker.Core.DTOs;
using Tracker.Data;

namespace Tracker.Api.Services;

public class SearchService(TrackerDbContext db) : ISearchService
{
    public async Task<SearchResultDto> SearchAsync(string query, int? projectId, int limit)
    {
        var results = new List<SearchResultItem>();

        var methods = await db.Methods
            .Where(m => m.RemovedAt == null
                && (m.CurrentName.Contains(query) || m.OriginalName.Contains(query))
                && (!projectId.HasValue || m.File.ProjectId == projectId.Value))
            .Include(m => m.File)
            .Take(limit)
            .Select(m => new SearchResultItem("method", m.Id, m.CurrentName, m.File.RelativePath, m.Status.ToString(), m.StartLine))
            .ToListAsync();

        var files = await db.Files
            .Where(f => f.RemovedAt == null
                && f.RelativePath.Contains(query)
                && (!projectId.HasValue || f.ProjectId == projectId.Value))
            .Take(limit)
            .Select(f => new SearchResultItem("file", f.Id, f.RelativePath, f.RelativePath, null, null))
            .ToListAsync();

        var classes = await db.Classes
            .Where(c => c.RemovedAt == null && c.Name.Contains(query))
            .Include(c => c.File)
            .Take(limit)
            .Select(c => new SearchResultItem("class", c.Id, c.Name, c.File.RelativePath, null, c.StartLine))
            .ToListAsync();

        results.AddRange(methods);
        results.AddRange(files);
        results.AddRange(classes);

        return new SearchResultDto(results.Take(limit).ToList(), results.Count);
    }
}
