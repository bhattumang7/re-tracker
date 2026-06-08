using Tracker.Core.Enums;

namespace Tracker.Core.DTOs;

public record MethodSummaryDto(
    int Id,
    string CurrentName,
    string OriginalName,
    string ReturnType,
    MigrationStatus Status,
    string? StatusComment,
    int FileId,
    string FilePath,
    int StartLine,
    int StartColumn
);

public record MethodDetailDto(
    int Id,
    string CurrentName,
    string OriginalName,
    string ReturnType,
    MigrationStatus Status,
    string? StatusComment,
    int FileId,
    string FilePath,
    int? ClassId,
    string? ClassName,
    int StartLine, int StartColumn,
    int EndLine,   int EndColumn,
    int BodyStartLine, int BodyStartColumn,
    int BodyEndLine,   int BodyEndColumn,
    List<MethodParameterDto> Parameters,
    List<MethodSummaryDto>   Callers,
    List<MethodSummaryDto>   Callees,
    List<RenameHistoryDto>   RenameHistory
);

public record MethodParameterDto(
    int Id,
    string CurrentName,
    string OriginalName,
    string Type,
    int Ordinal,
    int StartLine, int StartColumn,
    int EndLine,   int EndColumn
);

public record UpdateStatusRequest(MigrationStatus Status, string? Comment);

public record RenameRequest(string NewName, string? Comment = null);
