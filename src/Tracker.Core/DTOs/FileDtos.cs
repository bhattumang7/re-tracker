namespace Tracker.Core.DTOs;

public record FileDto(
    int Id,
    int ProjectId,
    string RelativePath,
    DateTime LastScannedAt,
    int TotalMethods,
    int DoneMethods,
    double Progress
);

public record FileDetailDto(
    int Id,
    int ProjectId,
    string RelativePath,
    DateTime LastScannedAt,
    List<MethodSummaryDto> Methods
);
