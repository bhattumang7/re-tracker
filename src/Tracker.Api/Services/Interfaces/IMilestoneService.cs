using Tracker.Core.DTOs;
using Tracker.Core.Enums;

namespace Tracker.Api.Services.Interfaces;

public interface IMilestoneService
{
    Task<List<MilestoneDto>> ListAsync(int? projectId);
    Task<List<MilestoneTreeDto>> GetTreeAsync(int? projectId);
    Task<MilestoneTreeDto?> GetSubtreeAsync(int id);
    Task<MilestoneDto?> GetAsync(int id);
    Task<PagedResult<MethodSummaryDto>> GetMethodsAsync(int id, MigrationStatus? status, int page, int pageSize);
    Task<MethodDetailDto?> GetNextAsync(int id);
    Task<GraphDto> GetGraphAsync(int id);
    Task<MilestoneDto> CreateAsync(string name, string? description, int projectId, int? parentId, int sortOrder);
    Task<bool> AddMethodAsync(int milestoneId, int methodId);
    Task<int> ScopeToRootAsync(int milestoneId, int rootMethodId);
    Task<List<CallTreeNodeDto>> GetCallTreeAsync(int milestoneId);
}
