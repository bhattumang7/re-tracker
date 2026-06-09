using Tracker.Core.DTOs;
using Tracker.Core.Enums;

namespace Tracker.Api.Services.Interfaces;

public interface IMethodService
{
    Task<PagedResult<MethodSummaryDto>> ListAsync(MigrationStatus? status, int? fileId, int? classId, string? nameContains, int page, int pageSize);
    Task<MethodDetailDto?> GetDetailAsync(int id);
    Task<MethodDetailDto?> GetNextAsync(int? projectId);
    Task<MethodSummaryDto?> UpdateStatusAsync(int id, MigrationStatus status, string? comment);
    Task<MethodSummaryDto?> SetPortAsync(int id, string? portedName, string? portedPath);
    Task<MethodSummaryDto?> RenameAsync(int id, string newName, string? comment);
    Task<List<MethodSummaryDto>> GetCallersAsync(int id);
    Task<List<MethodSummaryDto>> GetCalleesAsync(int id);
}
