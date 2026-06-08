namespace Tracker.Core.DTOs;

public record ScanJobDto(Guid JobId);

public record ScanStatusDto(Guid JobId, int Total, int Processed, bool Complete, string? Error);
