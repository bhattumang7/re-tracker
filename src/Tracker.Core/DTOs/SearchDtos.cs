namespace Tracker.Core.DTOs;

public record SearchResultItem(string Type, int Id, string Name, string? FilePath, string? Status, int? Line);

public record SearchResultDto(List<SearchResultItem> Items, int TotalCount);
