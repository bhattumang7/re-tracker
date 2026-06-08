using Tracker.Core.Models;
using Tracker.Data.Entities;

namespace Tracker.Data;

/// <summary>
/// 3-tier matching strategy used by the rescan reconciler to link parsed symbols
/// back to existing Method records even after a Ghidra rename.
/// </summary>
public static class MethodMatcher
{
    public const int DefaultLineTolerance = 5;

    // Tier 1: OriginalName  — unmodified method; name matches what the decompiler produced
    // Tier 2: CurrentName   — tracker already recorded the rename; file now reflects it
    // Tier 3: Proximity     — method shifted in the file; match by start-line within tolerance
    //                         only when exactly one candidate is in range (avoids false matches)
    public static Method? FindMatch(
        IEnumerable<Method> pool,
        ParsedMethod parsed,
        HashSet<int> alreadyMatched,
        int lineTolerance = DefaultLineTolerance)
    {
        var candidates = pool.Where(e => !alreadyMatched.Contains(e.Id));

        var match = candidates.FirstOrDefault(e => e.OriginalName == parsed.Name);
        if (match is not null) return match;

        match = candidates.FirstOrDefault(e => e.CurrentName == parsed.Name);
        if (match is not null) return match;

        var nearby = candidates
            .Where(e => Math.Abs(e.StartLine - parsed.StartLine) <= lineTolerance)
            .ToList();

        return nearby.Count == 1 ? nearby[0] : null;
    }
}
