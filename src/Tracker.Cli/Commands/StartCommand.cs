using System.CommandLine;
using Tracker.Core.Enums;

namespace Tracker.Cli.Commands;

public static class StartCommand
{
    public static Command Build()
    {
        var nameArg = new Argument<string>("name|id") { Description = "Method name or numeric ID" };

        var cmd = new Command("start", "Mark a method as InProgress");
        cmd.Add(nameArg);
        cmd.SetAction(async result =>
            await SharedHelpers.SetStatus(result.GetValue(nameArg)!, MigrationStatus.InProgress, null));

        return cmd;
    }
}
