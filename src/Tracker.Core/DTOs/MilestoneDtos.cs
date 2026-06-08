using Tracker.Core.Enums;

namespace Tracker.Core.DTOs;

// A node in a milestone's call tree. A function appears once per calling path
// (duplicates across branches are intentional). Cyclic=true marks a node that
// is its own ancestor — recursion is cut there and Children is empty.
public record CallTreeNodeDto(
    int Id,
    string CurrentName,
    MigrationStatus Status,
    string FilePath,
    int StartLine,
    bool Cyclic,
    List<CallTreeNodeDto> Children
);

public record MilestoneDto(
    int Id,
    int? ParentId,
    string Name,
    string? Description,
    int SortOrder,
    int TotalMethods,
    int DoneMethods,
    double Progress,
    Dictionary<string, int> ByStatus
);

public record MilestoneTreeDto(
    int Id,
    int? ParentId,
    string Name,
    string? Description,
    int TotalMethods,
    int DoneMethods,
    double Progress,
    List<MilestoneTreeDto> Children
);
