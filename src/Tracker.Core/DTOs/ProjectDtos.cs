namespace Tracker.Core.DTOs;

public record ProjectDto(int Id, string Name, string RootPath, string LanguageName, DateTime? LastScannedAt);

public record CreateProjectRequest(string Name, string RootPath, int LanguageId);
