namespace Tracker.Data.Entities;

public class Milestone
{
    public int     Id          { get; set; }
    public int     ProjectId   { get; set; }
    public int?    ParentId    { get; set; }
    public string  Name        { get; set; } = "";
    public string? Description { get; set; }
    public int     SortOrder   { get; set; }

    public Project                Project  { get; set; } = null!;
    public Milestone?             Parent   { get; set; }
    public ICollection<Milestone> Children { get; set; } = [];
    public ICollection<MilestoneMethod> MilestoneMethods { get; set; } = [];
}
