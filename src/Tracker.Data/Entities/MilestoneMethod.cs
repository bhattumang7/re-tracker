namespace Tracker.Data.Entities;

public class MilestoneMethod
{
    public int MilestoneId { get; set; }
    public int MethodId    { get; set; }
    public DateTime AddedAt { get; set; }

    public Milestone Milestone { get; set; } = null!;
    public Method    Method    { get; set; } = null!;
}
