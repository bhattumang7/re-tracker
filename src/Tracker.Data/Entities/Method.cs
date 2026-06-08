using Tracker.Core.Enums;

namespace Tracker.Data.Entities;

public class Method
{
    public int             Id            { get; set; }
    public int             FileId        { get; set; }
    public int?            ClassId       { get; set; }
    public string          CurrentName   { get; set; } = "";
    public string          OriginalName  { get; set; } = "";
    public string          ReturnType    { get; set; } = "";
    public MigrationStatus Status        { get; set; } = MigrationStatus.Pending;
    public string?         StatusComment { get; set; }

    // Where this symbol was re-implemented in the target codebase (language-agnostic).
    public string?         PortedName    { get; set; }
    public string?         PortedPath    { get; set; }

    // Full declaration span (return type through closing paren)
    public int StartLine   { get; set; }
    public int StartColumn { get; set; }
    public int EndLine     { get; set; }
    public int EndColumn   { get; set; }

    // Body span (opening brace to closing brace)
    public int BodyStartLine   { get; set; }
    public int BodyStartColumn { get; set; }
    public int BodyEndLine     { get; set; }
    public int BodyEndColumn   { get; set; }

    public DateTime  CreatedAt { get; set; }
    public DateTime  UpdatedAt { get; set; }
    public DateTime? RemovedAt { get; set; }

    public TrackedFile                  File             { get; set; } = null!;
    public TrackedClass?                Class            { get; set; }
    public ICollection<MethodParameter> Parameters       { get; set; } = [];
    public ICollection<MethodCall>      CallsAsCaller    { get; set; } = [];
    public ICollection<MethodCall>      CallsAsCallee    { get; set; } = [];
    public ICollection<MilestoneMethod> MilestoneMethods { get; set; } = [];
    public ICollection<RenameHistory>   RenameHistories  { get; set; } = [];
}
