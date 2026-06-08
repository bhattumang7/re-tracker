using System.Collections.Concurrent;
using Tracker.Core.DTOs;

namespace Tracker.Api.Services;

public class ScanProgressStore
{
    private readonly ConcurrentDictionary<Guid, ScanStatusDto> _jobs = new();

    public Guid Create()
    {
        var id = Guid.NewGuid();
        _jobs[id] = new ScanStatusDto(id, 0, 0, false, null);
        return id;
    }

    public void Update(Guid id, int total, int processed, bool complete, string? error = null)
        => _jobs[id] = new ScanStatusDto(id, total, processed, complete, error);

    public ScanStatusDto? Get(Guid id)
        => _jobs.TryGetValue(id, out var s) ? s : null;
}
