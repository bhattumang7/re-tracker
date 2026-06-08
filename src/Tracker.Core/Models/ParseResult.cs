namespace Tracker.Core.Models;

public record ParseResult(
    IReadOnlyList<ParsedClass>    Classes,
    IReadOnlyList<ParsedMethod>   Methods,
    IReadOnlyList<ParsedCallSite> CallSites
);

public record ParsedClass(
    string Name,
    int    StartLine,
    int    StartColumn,
    int    EndLine,
    int    EndColumn
);

public record ParsedMethod(
    string Name,
    string? ClassName,
    string ReturnType,
    int    StartLine,
    int    StartColumn,
    int    EndLine,
    int    EndColumn,
    int    BodyStartLine,
    int    BodyStartColumn,
    int    BodyEndLine,
    int    BodyEndColumn,
    IReadOnlyList<ParsedParameter> Parameters
);

public record ParsedParameter(
    string Name,
    string Type,
    int    Ordinal,
    int    StartLine,
    int    StartColumn,
    int    EndLine,
    int    EndColumn
);

public record ParsedCallSite(
    string CallerName,
    string CalleeName,
    int    CallLine,
    int    CallColumn
);
