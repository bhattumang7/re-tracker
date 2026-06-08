using Tracker.Core.DTOs;

namespace Tracker.Api.Services.Interfaces;

public interface IFileService
{
    Task<List<FileDto>> ListAsync(int? projectId);
    Task<FileDetailDto?> GetAsync(int id);
}
