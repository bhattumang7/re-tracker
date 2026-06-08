using Microsoft.EntityFrameworkCore;
using Tracker.Api.Services.Interfaces;
using Tracker.Core.DTOs;
using Tracker.Core.Enums;
using Tracker.Data;
using Tracker.Data.Entities;

namespace Tracker.Api.Services;

public class MilestoneService(TrackerDbContext db) : IMilestoneService
{
    private static readonly MigrationStatus[] TerminalStatuses =
        [MigrationStatus.Done, MigrationStatus.Skipped, MigrationStatus.NeedsReview];

    public async Task<List<MilestoneDto>> ListAsync(int? projectId)
    {
        var q = db.Milestones.Include(m => m.MilestoneMethods).ThenInclude(mm => mm.Method).AsQueryable();
        if (projectId.HasValue) q = q.Where(m => m.ProjectId == projectId.Value);
        return (await q.OrderBy(m => m.SortOrder).ToListAsync()).Select(ToDto).ToList();
    }

    public async Task<List<MilestoneTreeDto>> GetTreeAsync(int? projectId)
    {
        var all = await db.Milestones
            .Include(m => m.MilestoneMethods).ThenInclude(mm => mm.Method)
            .Where(m => !projectId.HasValue || m.ProjectId == projectId.Value)
            .OrderBy(m => m.SortOrder)
            .ToListAsync();

        return BuildTree(all, null);
    }

    public async Task<MilestoneTreeDto?> GetSubtreeAsync(int id)
    {
        var root = await db.Milestones
            .Include(m => m.MilestoneMethods).ThenInclude(mm => mm.Method)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (root is null) return null;

        // Load all descendants
        var all = await LoadDescendantsAsync(root.ProjectId, id);
        all.Insert(0, root);
        return BuildTree(all, null).FirstOrDefault(t => t.Id == id);
    }

    public async Task<MilestoneDto?> GetAsync(int id)
    {
        var m = await db.Milestones
            .Include(m => m.MilestoneMethods).ThenInclude(mm => mm.Method)
            .FirstOrDefaultAsync(m => m.Id == id);
        return m is null ? null : ToDto(m);
    }

    public async Task<PagedResult<MethodSummaryDto>> GetMethodsAsync(int id, MigrationStatus? status, int page, int pageSize)
    {
        var q = db.MilestoneMethods
            .Where(mm => mm.MilestoneId == id && mm.Method.RemovedAt == null)
            .Select(mm => mm.Method)
            .Include(m => m.File)
            .AsQueryable();

        if (status.HasValue) q = q.Where(m => m.Status == status.Value);

        var total = await q.CountAsync();
        var items = await q.OrderBy(m => m.CurrentName).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new MethodSummaryDto(m.Id, m.CurrentName, m.OriginalName, m.ReturnType,
                m.Status, m.StatusComment, m.FileId, m.File.RelativePath, m.StartLine, m.StartColumn))
            .ToListAsync();

        return new PagedResult<MethodSummaryDto>(items, total, page, pageSize);
    }

    public async Task<MethodDetailDto?> GetNextAsync(int milestoneId)
    {
        var methodIds = await db.MilestoneMethods
            .Where(mm => mm.MilestoneId == milestoneId && mm.Method.RemovedAt == null
                && !TerminalStatuses.Contains(mm.Method.Status))
            .Select(mm => mm.MethodId)
            .ToListAsync();

        if (methodIds.Count == 0) return null;

        // Find the method with fewest unresolved internal callees (topological leaf)
        int? bestId = null;
        int  bestUnresolved = int.MaxValue;

        foreach (var mId in methodIds)
        {
            var unresolvedCallees = await db.MethodCalls
                .Where(c => c.CallerMethodId == mId && c.CalleeMethodId != null
                    && !TerminalStatuses.Contains(c.CalleeMethod!.Status)
                    && c.CalleeMethod.RemovedAt == null)
                .CountAsync();

            if (unresolvedCallees < bestUnresolved)
            {
                bestUnresolved = unresolvedCallees;
                bestId = mId;
                if (unresolvedCallees == 0) break;
            }
        }

        if (bestId is null) return null;

        return await new MethodService(db).GetDetailAsync(bestId.Value);
    }

    public async Task<GraphDto> GetGraphAsync(int milestoneId)
    {
        var methods = await db.MilestoneMethods
            .Where(mm => mm.MilestoneId == milestoneId && mm.Method.RemovedAt == null)
            .Include(mm => mm.Method.File)
            .Select(mm => mm.Method)
            .ToListAsync();

        var methodIds = methods.Select(m => m.Id).ToHashSet();

        var edges = await db.MethodCalls
            .Where(c => methodIds.Contains(c.CallerMethodId)
                && c.CalleeMethodId != null
                && methodIds.Contains(c.CalleeMethodId.Value))
            .Select(c => new GraphEdgeDto(c.Id, c.CallerMethodId, c.CalleeMethodId!.Value, c.CallLine, c.CallColumn))
            .ToListAsync();

        var nodes = methods.Select(m => new GraphNodeDto(
            m.Id, m.CurrentName, m.OriginalName, m.Status, m.FileId, m.File.RelativePath)).ToList();

        return new GraphDto(nodes, edges);
    }

    public async Task<MilestoneDto> CreateAsync(string name, string? description, int projectId, int? parentId, int sortOrder)
    {
        var ms = new Milestone
        {
            Name        = name,
            Description = description,
            ProjectId   = projectId,
            ParentId    = parentId,
            SortOrder   = sortOrder
        };
        db.Milestones.Add(ms);
        await db.SaveChangesAsync();
        return ToDto(ms);
    }

    public async Task<bool> AddMethodAsync(int milestoneId, int methodId)
    {
        var exists = await db.MilestoneMethods.AnyAsync(mm => mm.MilestoneId == milestoneId && mm.MethodId == methodId);
        if (exists) return false;
        db.MilestoneMethods.Add(new MilestoneMethod { MilestoneId = milestoneId, MethodId = methodId, AddedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Define a milestone as a single top-level function's dependency subtree:
    /// walk callee edges breadth-first from the root and make exactly those
    /// functions the milestone's members (replacing any existing membership).
    /// Combined with GetNextAsync, this gives the "pick a top-level function,
    /// walk it depth-first (leaf-first)" workflow.
    /// </summary>
    public async Task<int> ScopeToRootAsync(int milestoneId, int rootMethodId)
    {
        // Adjacency: caller -> internal callees.
        var edges = await db.MethodCalls
            .Where(c => c.CalleeMethodId != null)
            .Select(c => new { c.CallerMethodId, Callee = c.CalleeMethodId!.Value })
            .ToListAsync();

        var adj = new Dictionary<int, List<int>>();
        foreach (var e in edges)
        {
            if (!adj.TryGetValue(e.CallerMethodId, out var list))
                adj[e.CallerMethodId] = list = new List<int>();
            list.Add(e.Callee);
        }

        // BFS over callees from the root.
        var reachable = new HashSet<int> { rootMethodId };
        var queue = new Queue<int>();
        queue.Enqueue(rootMethodId);
        while (queue.Count > 0)
        {
            var n = queue.Dequeue();
            if (!adj.TryGetValue(n, out var callees)) continue;
            foreach (var c in callees)
                if (reachable.Add(c)) queue.Enqueue(c);
        }

        var memberIds = await db.Methods
            .Where(m => m.RemovedAt == null && reachable.Contains(m.Id))
            .Select(m => m.Id)
            .ToListAsync();

        await db.MilestoneMethods.Where(mm => mm.MilestoneId == milestoneId).ExecuteDeleteAsync();
        db.MilestoneMethods.AddRange(memberIds.Select(id => new MilestoneMethod
        {
            MilestoneId = milestoneId,
            MethodId    = id,
            AddedAt     = DateTime.UtcNow
        }));
        await db.SaveChangesAsync();
        return memberIds.Count;
    }

    // --- helpers ---

    private static List<MilestoneTreeDto> BuildTree(List<Milestone> all, int? parentId)
    {
        return all
            .Where(m => m.ParentId == parentId)
            .OrderBy(m => m.SortOrder)
            .Select(m =>
            {
                var dto  = ToDto(m);
                var children = BuildTree(all, m.Id);
                return new MilestoneTreeDto(dto.Id, dto.ParentId, dto.Name, dto.Description,
                    dto.TotalMethods, dto.DoneMethods, dto.Progress, children);
            })
            .ToList();
    }

    private async Task<List<Milestone>> LoadDescendantsAsync(int projectId, int rootId)
    {
        return await db.Milestones
            .Include(m => m.MilestoneMethods).ThenInclude(mm => mm.Method)
            .Where(m => m.ProjectId == projectId)
            .ToListAsync();
    }

    private static MilestoneDto ToDto(Milestone m)
    {
        var active = m.MilestoneMethods.Where(mm => mm.Method?.RemovedAt == null).ToList();
        int total  = active.Count;
        int done   = active.Count(mm => mm.Method?.Status == MigrationStatus.Done);
        double pct = total > 0 ? Math.Round((double)done / total * 100, 1) : 0;
        return new MilestoneDto(m.Id, m.ParentId, m.Name, m.Description, m.SortOrder, total, done, pct);
    }
}
