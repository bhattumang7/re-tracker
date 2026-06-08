namespace Tracker.Core.DTOs;

public record MilestoneDto(
    int Id,
    int? ParentId,
    string Name,
    string? Description,
    int SortOrder,
    int TotalMethods,
    int DoneMethods,
    double Progress
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
