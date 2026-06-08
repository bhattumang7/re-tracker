using Tracker.Core.DTOs;

namespace Tracker.Api.Services.Interfaces;

public interface IScanService
{
    Task<ScanJobDto> TriggerScanAsync(int projectId);
    ScanStatusDto? GetStatus(Guid jobId);
}
