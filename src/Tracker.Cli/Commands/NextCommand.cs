using System.CommandLine;
using Microsoft.EntityFrameworkCore;
using Tracker.Cli.Infrastructure;
using Tracker.Core.Enums;

namespace Tracker.Cli.Commands;

public static class NextCommand
{
    private static readonly MigrationStatus[] Terminal =
        [MigrationStatus.Done, MigrationStatus.Skipped, MigrationStatus.NeedsReview];

    public static Command Build()
    {
        var projectOpt   = new Option<int?>("--project")   { Description = "Project ID" };
        var milestoneOpt = new Option<int?>("--milestone") { Description = "Milestone ID" };

        var cmd = new Command("next", "Show the next recommended method to rename");
        cmd.Add(projectOpt);
        cmd.Add(milestoneOpt);

        cmd.SetAction(async result =>
        {
            var project   = result.GetValue(projectOpt);
            var milestone = result.GetValue(milestoneOpt);

            await using var db = CliDbContextFactory.Create();
            var q = db.Methods
                .Include(m => m.File)
                .Include(m => m.CallsAsCaller).ThenInclude(c => c.CalleeMethod)
                .Where(m => m.RemovedAt == null && !Terminal.Contains(m.Status));

            if (project.HasValue)   q = q.Where(m => m.File.ProjectId == project.Value);
            if (milestone.HasValue) q = q.Where(m => m.MilestoneMethods.Any(mm => mm.MilestoneId == milestone.Value));

            var candidates = await q.ToListAsync();
            if (candidates.Count == 0) { Console.WriteLine("Nothing pending."); return; }

            var best = candidates
                .OrderBy(m => m.CallsAsCaller.Count(c =>
                    c.CalleeMethodId != null && !Terminal.Contains(c.CalleeMethod!.Status)))
                .First();

            Console.WriteLine("Next recommended:");
            OutputFormatter.PrintMethod(best);
        });

        return cmd;
    }
}
