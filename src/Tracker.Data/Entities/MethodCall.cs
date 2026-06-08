namespace Tracker.Data.Entities;

public class MethodCall
{
    public int     Id             { get; set; }
    public int     CallerMethodId { get; set; }
    public int?    CalleeMethodId { get; set; }  // null when callee not found in project
    public string  RawCalleeName  { get; set; } = "";
    public int     CallLine       { get; set; }
    public int     CallColumn     { get; set; }

    public Method  CallerMethod { get; set; } = null!;
    public Method? CalleeMethod { get; set; }
}
