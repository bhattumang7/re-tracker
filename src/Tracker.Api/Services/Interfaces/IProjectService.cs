using Tracker.Core.DTOs;

namespace Tracker.Api.Services.Interfaces;

public interface IProjectService
{
    Task<List<ProjectDto>> ListAsync();
    Task<ProjectDto?> GetAsync(int id);
    Task<ProjectDto> CreateAsync(string name, string rootPath, int languageId);
}
