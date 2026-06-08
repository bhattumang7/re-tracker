using Microsoft.EntityFrameworkCore;
using Tracker.Core.Enums;
using Tracker.Data;
using Tracker.Data.Entities;

namespace Tracker.Cli.Commands;

internal static class SharedHelpers
{
    public static async Task<Method?> ResolveMethod(TrackerDbContext db, string nameOrId)
        => int.TryParse(nameOrId, out var id)
            ? await db.Methods.Include(m => m.File).FirstOrDefaultAsync(m => m.Id == id && m.RemovedAt == null)
            : await db.Methods.Include(m => m.File).FirstOrDefaultAsync(m =>
                (m.CurrentName == nameOrId || m.OriginalName == nameOrId) && m.RemovedAt == null);

    public static async Task SetStatus(string nameOrId, MigrationStatus status, string? comment)
    {
        await using var db = Infrastructure.CliDbContextFactory.Create();
        var method = await ResolveMethod(db, nameOrId);
        if (method is null) { Infrastructure.OutputFormatter.PrintError($"Method '{nameOrId}' not found."); return; }

        method.Status        = status;
        method.StatusComment = comment;
        method.UpdatedAt     = DateTime.UtcNow;
        await db.SaveChangesAsync();
        Infrastructure.OutputFormatter.PrintSuccess($"{status}: {method.CurrentName}");
    }
}
