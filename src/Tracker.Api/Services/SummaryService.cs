using Microsoft.EntityFrameworkCore;
using Tracker.Api.Services.Interfaces;
using Tracker.Core.DTOs;
using Tracker.Core.Enums;
using Tracker.Data;

namespace Tracker.Api.Services;

public class SummaryService(TrackerDbContext db) : ISummaryService
{
    public async Task<SummaryDto> GetSummaryAsync(int? projectId = null)
    {
        var methodsQ = db.Methods.Where(m => m.RemovedAt == null);
        var filesQ   = db.Files.Where(f => f.RemovedAt == null);

        if (projectId.HasValue)
        {
            methodsQ = methodsQ.Where(m => m.File.ProjectId == projectId.Value);
            filesQ   = filesQ.Where(f => f.ProjectId == projectId.Value);
        }

        var totalMethods   = await methodsQ.CountAsync();
        var totalFiles     = await filesQ.CountAsync();
        var totalClasses   = await db.Classes.Where(c => c.RemovedAt == null).CountAsync();
        var totalMilestones = await db.Milestones.CountAsync();

        var byStatus = await methodsQ
            .GroupBy(m => m.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var byStatusDict = Enum.GetValues<MigrationStatus>()
            .ToDictionary(s => s.ToString(), s => 0);
        foreach (var g in byStatus)
            byStatusDict[g.Status.ToString()] = g.Count;

        int doneCount = byStatusDict.TryGetValue("Done", out var d) ? d : 0;
        double overall = totalMethods > 0 ? Math.Round((double)doneCount / totalMethods * 100, 1) : 0;

        var milestoneProgress = await db.Milestones
            .Select(ms => new MilestoneProgressDto(
                ms.Id,
                ms.Name,
                ms.MilestoneMethods.Count(mm => mm.Method.RemovedAt == null),
                ms.MilestoneMethods.Count(mm => mm.Method.RemovedAt == null && mm.Method.Status == MigrationStatus.Done),
                ms.MilestoneMethods.Count(mm => mm.Method.RemovedAt == null) == 0 ? 0
                    : Math.Round((double)ms.MilestoneMethods.Count(mm => mm.Method.RemovedAt == null && mm.Method.Status == MigrationStatus.Done)
                        / ms.MilestoneMethods.Count(mm => mm.Method.RemovedAt == null) * 100, 1)
            ))
            .ToListAsync();

        return new SummaryDto(totalMethods, totalFiles, totalClasses, totalMilestones, byStatusDict, overall, milestoneProgress);
    }
}
