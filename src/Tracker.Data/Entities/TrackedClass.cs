namespace Tracker.Data.Entities;

public class TrackedClass
{
    public int       Id          { get; set; }
    public int       FileId      { get; set; }
    public string    Name        { get; set; } = "";
    public int       StartLine   { get; set; }
    public int       StartColumn { get; set; }
    public int       EndLine     { get; set; }
    public int       EndColumn   { get; set; }
    public DateTime? RemovedAt   { get; set; }

    public TrackedFile         File    { get; set; } = null!;
    public ICollection<Method> Methods { get; set; } = [];
}
