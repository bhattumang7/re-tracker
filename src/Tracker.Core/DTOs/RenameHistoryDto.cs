namespace Tracker.Core.DTOs;

public record RenameHistoryDto(
    int Id,
    string EntityType,
    string OldName,
    string NewName,
    string? OldFilePath,
    string? NewFilePath,
    int? OldStartLine,
    int? OldStartColumn,
    int? NewStartLine,
    int? NewStartColumn,
    DateTime Timestamp,
    string? Comment
);
