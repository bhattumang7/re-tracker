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
        // No .Include here: File is projected explicitly below (EF emits the JOIN).
        // Include after a Select projection is invalid and 500s at runtime.
        var q = db.MilestoneMethods
            .Where(mm => mm.MilestoneId == id && mm.Method.RemovedAt == null)
            .Select(mm => mm.Method)
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

    /// <summary>
    /// Build the milestone's call tree: starting from its roots (members not
    /// called by any other member), expand callees recursively. A function
    /// appears once per calling path (duplicates across branches are intended).
    /// A node that is its own ancestor is flagged Cyclic and not expanded, and a
    /// global node cap prevents pathological blow-up on large/dense milestones.
    /// </summary>
    public async Task<List<CallTreeNodeDto>> GetCallTreeAsync(int milestoneId)
    {
        var members = await db.MilestoneMethods
            .Where(mm => mm.MilestoneId == milestoneId && mm.Method.RemovedAt == null)
            .Select(mm => new
            {
                mm.Method.Id,
                Name      = mm.Method.CurrentName,
                mm.Method.Status,
                FilePath  = mm.Method.File.RelativePath,
                mm.Method.StartLine
            })
            .ToListAsync();

        if (members.Count == 0) return new List<CallTreeNodeDto>();

        var info      = members.ToDictionary(m => m.Id);
        var memberIds = info.Keys.ToHashSet();

        var edges = await db.MethodCalls
            .Where(c => c.CalleeMethodId != null
                && memberIds.Contains(c.CallerMethodId)
                && memberIds.Contains(c.CalleeMethodId.Value))
            .Select(c => new { c.CallerMethodId, Callee = c.CalleeMethodId!.Value })
            .Distinct()
            .ToListAsync();

        var adj = new Dictionary<int, List<int>>();
        var calledByMember = new HashSet<int>();
        foreach (var e in edges)
        {
            if (!adj.TryGetValue(e.CallerMethodId, out var list))
                adj[e.CallerMethodId] = list = new List<int>();
            list.Add(e.Callee);
            calledByMember.Add(e.Callee);
        }
        foreach (var list in adj.Values)
            list.Sort((a, b) => string.Compare(info[a].Name, info[b].Name, StringComparison.Ordinal));

        // Roots: members nothing else in the milestone calls (in-degree 0).
        var roots = members
            .Where(m => !calledByMember.Contains(m.Id))
            .OrderBy(m => m.Name, StringComparer.Ordinal)
            .Select(m => m.Id)
            .ToList();
        // Fully cyclic membership has no in-degree-0 node — fall back to lowest id.
        if (roots.Count == 0)
            roots = members.OrderBy(m => m.Id).Select(m => m.Id).Take(1).ToList();

        const int maxNodes = 5000;
        int produced = 0;

        CallTreeNodeDto Build(int id, HashSet<int> ancestors)
        {
            var m = info[id];
            produced++;
            bool cyclic = ancestors.Contains(id);
            var children = new List<CallTreeNodeDto>();
            if (!cyclic && produced < maxNodes && adj.TryGetValue(id, out var callees))
            {
                ancestors.Add(id);
                foreach (var c in callees)
                {
                    if (produced >= maxNodes) break;
                    children.Add(Build(c, ancestors));
                }
                ancestors.Remove(id);
            }
            return new CallTreeNodeDto(id, m.Name, m.Status, m.FilePath, m.StartLine, cyclic, children);
        }

        return roots.Select(r => Build(r, new HashSet<int>())).ToList();
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
        var byStatus = active
            .GroupBy(mm => mm.Method!.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());
        return new MilestoneDto(m.Id, m.ParentId, m.Name, m.Description, m.SortOrder, total, done, pct, byStatus);
    }
}
