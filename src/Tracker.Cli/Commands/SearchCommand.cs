using System.CommandLine;
using Microsoft.EntityFrameworkCore;
using Tracker.Cli.Infrastructure;

namespace Tracker.Cli.Commands;

public static class SearchCommand
{
    public static Command Build()
    {
        var queryArg   = new Argument<string>("query")  { Description = "Search term" };
        var projectOpt = new Option<int?>("--project")  { Description = "Limit to project ID" };

        var cmd = new Command("search", "Search methods by name");
        cmd.Add(queryArg);
        cmd.Add(projectOpt);

        cmd.SetAction(async result =>
        {
            var query   = result.GetValue(queryArg)!;
            var project = result.GetValue(projectOpt);

            await using var db = CliDbContextFactory.Create();
            var q = db.Methods.Include(m => m.File)
                .Where(m => m.RemovedAt == null
                    && (m.CurrentName.Contains(query) || m.OriginalName.Contains(query)));

            if (project.HasValue) q = q.Where(m => m.File.ProjectId == project.Value);

            var results = await q.OrderBy(m => m.CurrentName).Take(50).ToListAsync();
            foreach (var m in results) OutputFormatter.PrintMethod(m);
            Console.WriteLine($"\n{results.Count} result(s)");
        });

        return cmd;
    }
}
