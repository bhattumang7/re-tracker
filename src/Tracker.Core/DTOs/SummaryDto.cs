namespace Tracker.Core.DTOs;

public record SummaryDto(
    int TotalMethods,
    int TotalFiles,
    int TotalClasses,
    int TotalMilestones,
    Dictionary<string, int> ByStatus,
    double OverallProgress,
    List<MilestoneProgressDto> MilestoneProgress
);

public record MilestoneProgressDto(int Id, string Name, int Total, int Done, double Progress);
