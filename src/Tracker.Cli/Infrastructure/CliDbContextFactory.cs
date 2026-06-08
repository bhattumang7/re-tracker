using Microsoft.EntityFrameworkCore;
using Tracker.Data;

namespace Tracker.Cli.Infrastructure;

public static class CliDbContextFactory
{
    public static TrackerDbContext Create()
    {
        var connStr = Environment.GetEnvironmentVariable("RETRACKER_CONN")
            ?? "Server=localhost,1433;Database=ReTracker;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;";

        var opts = new DbContextOptionsBuilder<TrackerDbContext>()
            .UseSqlServer(connStr)
            .Options;

        return new TrackerDbContext(opts);
    }
}
