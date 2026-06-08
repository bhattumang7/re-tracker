using System.CommandLine;
using Tracker.Core.Enums;

namespace Tracker.Cli.Commands;

public static class SkipCommand
{
    public static Command Build()
    {
        var nameArg   = new Argument<string>("name|id") { Description = "Method name or numeric ID" };
        var reasonOpt = new Option<string?>("--reason") { Description = "Reason for skipping" };

        var cmd = new Command("skip", "Mark a method as Skipped");
        cmd.Add(nameArg);
        cmd.Add(reasonOpt);
        cmd.SetAction(async result =>
            await SharedHelpers.SetStatus(result.GetValue(nameArg)!, MigrationStatus.Skipped, result.GetValue(reasonOpt)));

        return cmd;
    }
}
