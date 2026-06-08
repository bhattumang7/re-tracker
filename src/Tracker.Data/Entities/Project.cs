namespace Tracker.Data.Entities;

public class Project
{
    public int       Id            { get; set; }
    public int       LanguageId    { get; set; }
    public string    Name          { get; set; } = "";
    public string    RootPath      { get; set; } = "";
    public DateTime  CreatedAt     { get; set; }
    public DateTime? LastScannedAt { get; set; }

    public Language                Language   { get; set; } = null!;
    public ICollection<TrackedFile> Files     { get; set; } = [];
    public ICollection<Milestone>  Milestones { get; set; } = [];
}
