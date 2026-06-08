using System.CommandLine;
using Microsoft.EntityFrameworkCore;
using Tracker.Cli.Infrastructure;
using Tracker.Core.Enums;

namespace Tracker.Cli.Commands;

public static class StatusCommand
{
    public static Command Build()
    {
        var filterOpt  = new Option<MigrationStatus?>("--filter")  { Description = "Filter by status" };
        var fileOpt    = new Option<string?>("--file")             { Description = "Filter by file path substring" };
        var projectOpt = new Option<int?>("--project")             { Description = "Filter by project ID" };

        var cmd = new Command("status", "List methods with optional filters");
        cmd.Add(filterOpt);
        cmd.Add(fileOpt);
        cmd.Add(projectOpt);

        cmd.SetAction(async result =>
        {
            var filter  = result.GetValue(filterOpt);
            var file    = result.GetValue(fileOpt);
            var project = result.GetValue(projectOpt);

            await using var db = CliDbContextFactory.Create();
            var q = db.Methods.Include(m => m.File).Where(m => m.RemovedAt == null);

            if (filter.HasValue)              q = q.Where(m => m.Status == filter.Value);
            if (!string.IsNullOrEmpty(file))  q = q.Where(m => m.File.RelativePath.Contains(file));
            if (project.HasValue)             q = q.Where(m => m.File.ProjectId == project.Value);

            var methods = await q.OrderBy(m => m.File.RelativePath).ThenBy(m => m.StartLine).ToListAsync();
            foreach (var m in methods) OutputFormatter.PrintMethod(m);
            Console.WriteLine($"\n{methods.Count} method(s)");
        });

        return cmd;
    }
}
