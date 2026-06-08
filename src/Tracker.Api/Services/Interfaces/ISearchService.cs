using Tracker.Core.DTOs;

namespace Tracker.Api.Services.Interfaces;

public interface ISearchService
{
    Task<SearchResultDto> SearchAsync(string query, int? projectId, int limit);
}
