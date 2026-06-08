namespace Tracker.Data.Entities;

public class RenameHistory
{
    public int      Id             { get; set; }
    public int?     MethodId       { get; set; }
    public string   EntityType     { get; set; } = "";  // "Method", "Parameter", "File", "Class"
    public string   OldName        { get; set; } = "";
    public string   NewName        { get; set; } = "";
    public string?  OldFilePath    { get; set; }
    public string?  NewFilePath    { get; set; }
    public int?     OldStartLine   { get; set; }
    public int?     OldStartColumn { get; set; }
    public int?     NewStartLine   { get; set; }
    public int?     NewStartColumn { get; set; }
    public DateTime Timestamp      { get; set; }
    public string?  Comment        { get; set; }

    public Method? Method { get; set; }
}
