using System.CommandLine;
using Microsoft.EntityFrameworkCore;
using Tracker.Cli.Infrastructure;

namespace Tracker.Cli.Commands;

public static class InfoCommand
{
    public static Command Build()
    {
        var nameArg = new Argument<string>("name|id") { Description = "Method name or numeric ID" };

        var cmd = new Command("info", "Show full detail for a method");
        cmd.Add(nameArg);

        cmd.SetAction(async result =>
        {
            var nameOrId = result.GetValue(nameArg)!;
            await using var db = CliDbContextFactory.Create();
            var m = await SharedHelpers.ResolveMethod(db, nameOrId);
            if (m is null) { OutputFormatter.PrintError($"Method '{nameOrId}' not found."); return; }

            await db.Entry(m).Collection(x => x.Parameters).LoadAsync();
            await db.Entry(m).Collection(x => x.CallsAsCaller).Query()
                .Include(c => c.CalleeMethod!.File).LoadAsync();
            await db.Entry(m).Collection(x => x.RenameHistories).LoadAsync();

            Console.WriteLine($"ID:           {m.Id}");
            Console.WriteLine($"Current name: {m.CurrentName}");
            Console.WriteLine($"Original:     {m.OriginalName}");
            Console.WriteLine($"Return type:  {m.ReturnType}");
            Console.WriteLine($"Status:       {m.Status}");
            Console.WriteLine($"File:         {m.File?.RelativePath}:{m.StartLine}");
            Console.WriteLine($"Comment:      {m.StatusComment ?? "(none)"}");

            if (m.Parameters.Count > 0)
            {
                Console.WriteLine("\nParameters:");
                foreach (var p in m.Parameters.OrderBy(p => p.Ordinal))
                    Console.WriteLine($"  [{p.Ordinal}] {p.Type} {p.CurrentName}  ({p.StartLine}:{p.StartColumn})");
            }

            if (m.CallsAsCaller.Any())
            {
                Console.WriteLine("\nCalls:");
                foreach (var c in m.CallsAsCaller.Where(c => c.CalleeMethod is not null).Take(10))
                    Console.WriteLine($"  → {c.CalleeMethod!.CurrentName}  ({c.CalleeMethod.File?.RelativePath}:{c.CallLine})");
            }

            if (m.RenameHistories.Any())
            {
                Console.WriteLine("\nRename history:");
                foreach (var h in m.RenameHistories.OrderByDescending(h => h.Timestamp).Take(5))
                    Console.WriteLine($"  {h.Timestamp:yyyy-MM-dd HH:mm} {h.OldName} → {h.NewName}  {h.Comment}");
            }
        });

        return cmd;
    }
}
