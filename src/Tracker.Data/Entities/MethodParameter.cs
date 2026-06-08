namespace Tracker.Data.Entities;

public class MethodParameter
{
    public int    Id           { get; set; }
    public int    MethodId     { get; set; }
    public string CurrentName  { get; set; } = "";
    public string OriginalName { get; set; } = "";
    public string Type         { get; set; } = "";
    public int    Ordinal      { get; set; }
    public int    StartLine    { get; set; }
    public int    StartColumn  { get; set; }
    public int    EndLine      { get; set; }
    public int    EndColumn    { get; set; }

    public Method Method { get; set; } = null!;
}
