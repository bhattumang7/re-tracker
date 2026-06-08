using Tracker.Core.DTOs;

namespace Tracker.Api.Services.Interfaces;

public interface ISummaryService
{
    Task<SummaryDto> GetSummaryAsync(int? projectId = null);
}
