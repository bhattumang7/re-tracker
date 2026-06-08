using Microsoft.EntityFrameworkCore;
using Tracker.Api.Services.Interfaces;
using Tracker.Core.DTOs;
using Tracker.Core.Enums;
using Tracker.Data;
using Tracker.Data.Entities;

namespace Tracker.Api.Services;

public class MethodService(TrackerDbContext db) : IMethodService
{
    public async Task<PagedResult<MethodSummaryDto>> ListAsync(
        MigrationStatus? status, int? fileId, int? classId, string? nameContains, int page, int pageSize)
    {
        var q = db.Methods
            .Where(m => m.RemovedAt == null)
            .Include(m => m.File)
            .AsQueryable();

        if (status.HasValue)     q = q.Where(m => m.Status == status.Value);
        if (fileId.HasValue)     q = q.Where(m => m.FileId == fileId.Value);
        if (classId.HasValue)    q = q.Where(m => m.ClassId == classId.Value);
        if (!string.IsNullOrEmpty(nameContains))
            q = q.Where(m => m.CurrentName.Contains(nameContains) || m.OriginalName.Contains(nameContains));

        var total = await q.CountAsync();
        var items = await q
            .OrderBy(m => m.CurrentName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => ToSummary(m))
            .ToListAsync();

        return new PagedResult<MethodSummaryDto>(items, total, page, pageSize);
    }

    public async Task<MethodDetailDto?> GetDetailAsync(int id)
    {
        var m = await db.Methods
            .Where(m => m.Id == id && m.RemovedAt == null)
            .Include(m => m.File)
            .Include(m => m.Class)
            .Include(m => m.Parameters.OrderBy(p => p.Ordinal))
            .FirstOrDefaultAsync();

        if (m is null) return null;

        // Project explicitly through the navigation so EF emits a SQL JOIN for
        // File.RelativePath. (A projection through ToSummary drops the Include,
        // leaving File null — hence the NRE once call edges exist.)
        var callers = await db.MethodCalls
            .Where(c => c.CalleeMethodId == id)
            .Select(c => c.CallerMethod)
            .Distinct()
            .Select(m => new MethodSummaryDto(
                m.Id, m.CurrentName, m.OriginalName, m.ReturnType, m.Status, m.StatusComment,
                m.FileId, m.File.RelativePath, m.StartLine, m.StartColumn))
            .ToListAsync();

        var callees = await db.MethodCalls
            .Where(c => c.CallerMethodId == id && c.CalleeMethodId != null)
            .Select(c => c.CalleeMethod!)
            .Distinct()
            .Select(m => new MethodSummaryDto(
                m.Id, m.CurrentName, m.OriginalName, m.ReturnType, m.Status, m.StatusComment,
                m.FileId, m.File.RelativePath, m.StartLine, m.StartColumn))
            .ToListAsync();

        var history = await db.RenameHistories
            .Where(h => h.MethodId == id)
            .OrderByDescending(h => h.Timestamp)
            .Take(20)
            .Select(h => new RenameHistoryDto(h.Id, h.EntityType, h.OldName, h.NewName,
                h.OldFilePath, h.NewFilePath, h.OldStartLine, h.OldStartColumn,
                h.NewStartLine, h.NewStartColumn, h.Timestamp, h.Comment))
            .ToListAsync();

        return new MethodDetailDto(
            m.Id, m.CurrentName, m.OriginalName, m.ReturnType, m.Status, m.StatusComment,
            m.FileId, m.File.RelativePath, m.ClassId, m.Class?.Name,
            m.StartLine, m.StartColumn, m.EndLine, m.EndColumn,
            m.BodyStartLine, m.BodyStartColumn, m.BodyEndLine, m.BodyEndColumn,
            m.Parameters.Select(p => new MethodParameterDto(p.Id, p.CurrentName, p.OriginalName,
                p.Type, p.Ordinal, p.StartLine, p.StartColumn, p.EndLine, p.EndColumn)).ToList(),
            callers, callees, history,
            m.PortedName, m.PortedPath
        );
    }

    public async Task<MethodSummaryDto?> SetPortAsync(int id, string? portedName, string? portedPath)
    {
        var m = await db.Methods.Include(m => m.File).FirstOrDefaultAsync(m => m.Id == id && m.RemovedAt == null);
        if (m is null) return null;

        m.PortedName = string.IsNullOrWhiteSpace(portedName) ? null : portedName.Trim();
        m.PortedPath = string.IsNullOrWhiteSpace(portedPath) ? null : portedPath.Trim();
        m.UpdatedAt  = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ToSummary(m);
    }

    public async Task<MethodSummaryDto?> UpdateStatusAsync(int id, MigrationStatus status, string? comment)
    {
        var m = await db.Methods.Include(m => m.File).FirstOrDefaultAsync(m => m.Id == id && m.RemovedAt == null);
        if (m is null) return null;

        m.Status        = status;
        m.StatusComment = comment;
        m.UpdatedAt     = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ToSummary(m);
    }

    public async Task<MethodSummaryDto?> RenameAsync(int id, string newName, string? comment)
    {
        var m = await db.Methods.Include(m => m.File).FirstOrDefaultAsync(m => m.Id == id && m.RemovedAt == null);
        if (m is null) return null;

        db.RenameHistories.Add(new RenameHistory
        {
            MethodId       = m.Id,
            EntityType     = "Method",
            OldName        = m.CurrentName,
            NewName        = newName,
            OldStartLine   = m.StartLine,
            OldStartColumn = m.StartColumn,
            NewStartLine   = m.StartLine,
            NewStartColumn = m.StartColumn,
            Timestamp      = DateTime.UtcNow,
            Comment        = comment
        });

        m.CurrentName = newName;
        m.UpdatedAt   = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ToSummary(m);
    }

    public async Task<List<MethodSummaryDto>> GetCallersAsync(int id)
        => await db.MethodCalls
            .Where(c => c.CalleeMethodId == id)
            .Select(c => c.CallerMethod)
            .Distinct()
            .Select(m => new MethodSummaryDto(
                m.Id, m.CurrentName, m.OriginalName, m.ReturnType, m.Status, m.StatusComment,
                m.FileId, m.File.RelativePath, m.StartLine, m.StartColumn))
            .ToListAsync();

    public async Task<List<MethodSummaryDto>> GetCalleesAsync(int id)
        => await db.MethodCalls
            .Where(c => c.CallerMethodId == id && c.CalleeMethodId != null)
            .Select(c => c.CalleeMethod!)
            .Distinct()
            .Select(m => new MethodSummaryDto(
                m.Id, m.CurrentName, m.OriginalName, m.ReturnType, m.Status, m.StatusComment,
                m.FileId, m.File.RelativePath, m.StartLine, m.StartColumn))
            .ToListAsync();

    private static MethodSummaryDto ToSummary(Method m) => new(
        m.Id, m.CurrentName, m.OriginalName, m.ReturnType, m.Status, m.StatusComment,
        m.FileId, m.File.RelativePath, m.StartLine, m.StartColumn);
}
