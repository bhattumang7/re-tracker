using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tracker.Data;

// Used by EF Core CLI tooling (dotnet ef migrations add / database update)
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TrackerDbContext>
{
    public TrackerDbContext CreateDbContext(string[] args)
    {
        var connStr = Environment.GetEnvironmentVariable("RETRACKER_CONN")
            ?? "Server=localhost,1433;Database=ReTracker;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;";

        var opts = new DbContextOptionsBuilder<TrackerDbContext>()
            .UseSqlServer(connStr)
            .Options;

        return new TrackerDbContext(opts);
    }
}
