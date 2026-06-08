using Microsoft.EntityFrameworkCore;
using Tracker.Api.Services.Interfaces;
using Tracker.Core.DTOs;
using Tracker.Core.Enums;
using Tracker.Data;

namespace Tracker.Api.Services;

public class FileService(TrackerDbContext db) : IFileService
{
    public async Task<List<FileDto>> ListAsync(int? projectId)
    {
        var q = db.Files.Where(f => f.RemovedAt == null);
        if (projectId.HasValue) q = q.Where(f => f.ProjectId == projectId.Value);

        return await q
            .OrderBy(f => f.RelativePath)
            .Select(f => new FileDto(
                f.Id,
                f.ProjectId,
                f.RelativePath,
                f.LastScannedAt,
                f.Methods.Count(m => m.RemovedAt == null),
                f.Methods.Count(m => m.RemovedAt == null && m.Status == MigrationStatus.Done),
                f.Methods.Count(m => m.RemovedAt == null) == 0 ? 0
                    : Math.Round((double)f.Methods.Count(m => m.RemovedAt == null && m.Status == MigrationStatus.Done)
                        / f.Methods.Count(m => m.RemovedAt == null) * 100, 1)
            ))
            .ToListAsync();
    }

    public async Task<FileDetailDto?> GetAsync(int id)
    {
        var f = await db.Files
            .Where(f => f.Id == id && f.RemovedAt == null)
            .FirstOrDefaultAsync();

        if (f is null) return null;

        var methods = await db.Methods
            .Where(m => m.FileId == id && m.RemovedAt == null)
            .Include(m => m.File)
            .OrderBy(m => m.StartLine)
            .Select(m => new MethodSummaryDto(m.Id, m.CurrentName, m.OriginalName, m.ReturnType,
                m.Status, m.StatusComment, m.FileId, f.RelativePath, m.StartLine, m.StartColumn))
            .ToListAsync();

        return new FileDetailDto(f.Id, f.ProjectId, f.RelativePath, f.LastScannedAt, methods);
    }
}
