using Tracker.Core.Enums;

namespace Tracker.Core.DTOs;

public record GraphDto(List<GraphNodeDto> Nodes, List<GraphEdgeDto> Edges);

public record GraphNodeDto(
    int Id,
    string Label,
    string OriginalName,
    MigrationStatus Status,
    int FileId,
    string FilePath
);

public record GraphEdgeDto(int Id, int SourceId, int TargetId, int CallLine, int CallColumn);
