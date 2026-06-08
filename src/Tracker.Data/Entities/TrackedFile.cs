namespace Tracker.Data.Entities;

public class TrackedFile
{
    public int       Id            { get; set; }
    public int       ProjectId     { get; set; }
    public string    RelativePath  { get; set; } = "";
    public DateTime  LastScannedAt { get; set; }
    public DateTime? RemovedAt     { get; set; }

    public Project                 Project { get; set; } = null!;
    public ICollection<TrackedClass> Classes { get; set; } = [];
    public ICollection<Method>     Methods { get; set; } = [];
}
