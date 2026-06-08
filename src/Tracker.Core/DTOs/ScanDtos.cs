namespace Tracker.Core.DTOs;

public record ScanJobDto(Guid JobId);

public record ScanStatusDto(Guid JobId, int Total, int Processed, bool Complete, string? Error);

// Call-graph import: caller/callee are function names (current or original).
public record CallGraphEdgeDto(string Caller, string Callee);

public record CallGraphImportRequest(IReadOnlyList<CallGraphEdgeDto> Edges);

public record CallGraphImportResult(int EdgesReceived, int Inserted, int SkippedUnresolved, int SkippedSelfOrDuplicate);
