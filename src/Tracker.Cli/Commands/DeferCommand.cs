using System.CommandLine;
using Tracker.Core.Enums;

namespace Tracker.Cli.Commands;

public static class DeferCommand
{
    public static Command Build()
    {
        var nameArg    = new Argument<string>("name|id") { Description = "Method name or numeric ID" };
        var commentOpt = new Option<string?>("--comment") { Description = "Optional comment" };

        var cmd = new Command("defer", "Mark a method as Deferred");
        cmd.Add(nameArg);
        cmd.Add(commentOpt);
        cmd.SetAction(async result =>
            await SharedHelpers.SetStatus(result.GetValue(nameArg)!, MigrationStatus.Deferred, result.GetValue(commentOpt)));

        return cmd;
    }
}
