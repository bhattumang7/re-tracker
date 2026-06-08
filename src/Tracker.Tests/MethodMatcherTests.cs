using Tracker.Core.Models;
using Tracker.Data;
using Tracker.Data.Entities;

namespace Tracker.Tests;

public class MethodMatcherTests
{
    private static ParsedMethod Parsed(string name, int startLine = 10) =>
        new(name, null, "int", startLine, 0, startLine + 10, 0, startLine + 1, 0, startLine + 9, 0, []);

    private static Method Existing(int id, string originalName, string? currentName = null, int startLine = 10) =>
        new() { Id = id, OriginalName = originalName, CurrentName = currentName ?? originalName, StartLine = startLine };

    // --- Tier 1: OriginalName ---

    [Fact]
    public void Tier1_MatchesByOriginalName()
    {
        var pool    = new List<Method> { Existing(1, "sub_1234") };
        var matched = new HashSet<int>();

        var result = MethodMatcher.FindMatch(pool, Parsed("sub_1234"), matched);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public void Tier1_IsPreferredOverProximity()
    {
        // Two methods: one matches by name at a different line, one is nearby but wrong name.
        var pool = new List<Method>
        {
            Existing(1, "sub_1234", startLine: 50),
            Existing(2, "sub_9999", startLine: 11),
        };
        var matched = new HashSet<int>();

        var result = MethodMatcher.FindMatch(pool, Parsed("sub_1234", startLine: 10), matched);

        Assert.Equal(1, result!.Id);
    }

    // --- Tier 2: CurrentName ---

    [Fact]
    public void Tier2_MatchesByCurrentName_WhenOriginalDiffers()
    {
        // Tracker already recorded the rename (CurrentName = "processPacket"),
        // the file now has "processPacket" so OriginalName won't match.
        var pool    = new List<Method> { Existing(1, "sub_1234", currentName: "processPacket") };
        var matched = new HashSet<int>();

        var result = MethodMatcher.FindMatch(pool, Parsed("processPacket"), matched);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public void Tier2_NotUsed_WhenOriginalNameAlreadyMatches()
    {
        // If Tier 1 matches, Tier 2 is never reached — the same method shouldn't be returned twice.
        var pool    = new List<Method> { Existing(1, "processPacket", currentName: "processPacket") };
        var matched = new HashSet<int>();

        var result = MethodMatcher.FindMatch(pool, Parsed("processPacket"), matched);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    // --- Tier 3: Proximity ---

    [Fact]
    public void Tier3_MatchesByProximity_WhenNamesUnknown()
    {
        var pool    = new List<Method> { Existing(1, "sub_1234", startLine: 12) };
        var matched = new HashSet<int>();

        // startLine 10, existing is at 12 — within ±5
        var result = MethodMatcher.FindMatch(pool, Parsed("unknown_func", startLine: 10), matched);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public void Tier3_NoMatch_WhenMultipleCandidatesInRange()
    {
        var pool = new List<Method>
        {
            Existing(1, "sub_1234", startLine: 11),
            Existing(2, "sub_5678", startLine: 13),
        };
        var matched = new HashSet<int>();

        var result = MethodMatcher.FindMatch(pool, Parsed("unknown_func", startLine: 10), matched);

        Assert.Null(result);
    }

    [Fact]
    public void Tier3_NoMatch_WhenOutsideTolerance()
    {
        var pool    = new List<Method> { Existing(1, "sub_1234", startLine: 20) };
        var matched = new HashSet<int>();

        // startLine 10, existing is at 20 — outside ±5
        var result = MethodMatcher.FindMatch(pool, Parsed("unknown_func", startLine: 10), matched);

        Assert.Null(result);
    }

    [Fact]
    public void Tier3_ExactlyAtTolerance_Matches()
    {
        var pool    = new List<Method> { Existing(1, "sub_1234", startLine: 15) };
        var matched = new HashSet<int>();

        // diff = 5, exactly at boundary
        var result = MethodMatcher.FindMatch(pool, Parsed("unknown_func", startLine: 10), matched);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    // --- AlreadyMatched exclusion ---

    [Fact]
    public void AlreadyMatched_IsExcluded_FromAllTiers()
    {
        var pool    = new List<Method> { Existing(1, "sub_1234", startLine: 10) };
        var matched = new HashSet<int> { 1 };

        var result = MethodMatcher.FindMatch(pool, Parsed("sub_1234", startLine: 10), matched);

        Assert.Null(result);
    }

    // --- No match ---

    [Fact]
    public void ReturnsNull_WhenPoolIsEmpty()
    {
        var result = MethodMatcher.FindMatch([], Parsed("sub_1234"), []);
        Assert.Null(result);
    }

    [Fact]
    public void ReturnsNull_WhenNoTierMatches()
    {
        var pool    = new List<Method> { Existing(1, "sub_9999", startLine: 100) };
        var matched = new HashSet<int>();

        var result = MethodMatcher.FindMatch(pool, Parsed("sub_1234", startLine: 10), matched);

        Assert.Null(result);
    }
}
