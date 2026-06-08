using System.CommandLine;
using Tracker.Data.Entities;

namespace Tracker.Cli.Commands;

public static class RenameCommand
{
    public static Command Build()
    {
        var nameArg    = new Argument<string>("name|id")  { Description = "Method name or numeric ID" };
        var newNameArg = new Argument<string>("new-name") { Description = "New display name to record" };
        var commentOpt = new Option<string?>("--comment") { Description = "Optional comment" };

        var cmd = new Command("rename", "Record that a method has been renamed in the source");
        cmd.Add(nameArg);
        cmd.Add(newNameArg);
        cmd.Add(commentOpt);
        cmd.SetAction(async result =>
        {
            var nameOrId = result.GetValue(nameArg)!;
            var newName  = result.GetValue(newNameArg)!;
            var comment  = result.GetValue(commentOpt);

            await using var db = Infrastructure.CliDbContextFactory.Create();
            var method = await SharedHelpers.ResolveMethod(db, nameOrId);
            if (method is null)
            {
                Infrastructure.OutputFormatter.PrintError($"Method '{nameOrId}' not found.");
                return;
            }

            db.RenameHistories.Add(new RenameHistory
            {
                MethodId       = method.Id,
                EntityType     = "Method",
                OldName        = method.CurrentName,
                NewName        = newName,
                OldStartLine   = method.StartLine,
                OldStartColumn = method.StartColumn,
                NewStartLine   = method.StartLine,
                NewStartColumn = method.StartColumn,
                Timestamp      = DateTime.UtcNow,
                Comment        = comment
            });

            method.CurrentName = newName;
            method.UpdatedAt   = DateTime.UtcNow;
            await db.SaveChangesAsync();

            Infrastructure.OutputFormatter.PrintSuccess($"Renamed: {nameOrId} → {newName}");
        });

        return cmd;
    }
}
