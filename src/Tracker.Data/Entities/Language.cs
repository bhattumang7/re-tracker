namespace Tracker.Data.Entities;

public class Language
{
    public int    Id          { get; set; }
    public string Name        { get; set; } = "";  // "c", "csharp", "java"
    public string DisplayName { get; set; } = "";
    public string Extensions  { get; set; } = "";  // ".c,.h"

    public ICollection<Project> Projects { get; set; } = [];
}
