using System.CommandLine;
using Tracker.Core.Enums;

namespace Tracker.Cli.Commands;

public static class DoneCommand
{
    public static Command Build()
    {
        var nameArg    = new Argument<string>("name|id") { Description = "Method name or numeric ID" };
        var commentOpt = new Option<string?>("--comment") { Description = "Optional comment" };

        var cmd = new Command("done", "Mark a method as Done");
        cmd.Add(nameArg);
        cmd.Add(commentOpt);
        cmd.SetAction(async result =>
            await SharedHelpers.SetStatus(result.GetValue(nameArg)!, MigrationStatus.Done, result.GetValue(commentOpt)));

        return cmd;
    }
}
