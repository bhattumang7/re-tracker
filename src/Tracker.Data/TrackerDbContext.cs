using Microsoft.EntityFrameworkCore;
using Tracker.Core.Enums;
using Tracker.Data.Entities;

namespace Tracker.Data;

public class TrackerDbContext(DbContextOptions<TrackerDbContext> options) : DbContext(options)
{
    public DbSet<Language>        Languages        => Set<Language>();
    public DbSet<Project>         Projects         => Set<Project>();
    public DbSet<TrackedFile>     Files            => Set<TrackedFile>();
    public DbSet<TrackedClass>    Classes          => Set<TrackedClass>();
    public DbSet<Method>          Methods          => Set<Method>();
    public DbSet<MethodParameter> MethodParameters => Set<MethodParameter>();
    public DbSet<MethodCall>      MethodCalls      => Set<MethodCall>();
    public DbSet<RenameHistory>   RenameHistories  => Set<RenameHistory>();
    public DbSet<Milestone>       Milestones       => Set<Milestone>();
    public DbSet<MilestoneMethod> MilestoneMethods => Set<MilestoneMethod>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<MilestoneMethod>()
          .HasKey(mm => new { mm.MilestoneId, mm.MethodId });

        mb.Entity<MilestoneMethod>()
          .HasOne(mm => mm.Milestone)
          .WithMany(m => m.MilestoneMethods)
          .HasForeignKey(mm => mm.MilestoneId)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<MilestoneMethod>()
          .HasOne(mm => mm.Method)
          .WithMany(m => m.MilestoneMethods)
          .HasForeignKey(mm => mm.MethodId)
          .OnDelete(DeleteBehavior.Restrict);

        // MethodCall has two FKs to Method — disable cascade to avoid cycle
        mb.Entity<MethodCall>()
          .HasOne(c => c.CallerMethod)
          .WithMany(m => m.CallsAsCaller)
          .HasForeignKey(c => c.CallerMethodId)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<MethodCall>()
          .HasOne(c => c.CalleeMethod)
          .WithMany(m => m.CallsAsCallee)
          .HasForeignKey(c => c.CalleeMethodId)
          .OnDelete(DeleteBehavior.Restrict);

        // Milestone self-referencing tree
        mb.Entity<Milestone>()
          .HasOne(m => m.Parent)
          .WithMany(m => m.Children)
          .HasForeignKey(m => m.ParentId)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Method>()
          .Property(m => m.Status)
          .HasConversion<int>();

        // Re-scan match key: original name + file (unique, excluding soft-deleted)
        mb.Entity<Method>()
          .HasIndex(m => new { m.FileId, m.OriginalName })
          .IsUnique()
          .HasFilter("[RemovedAt] IS NULL");

        mb.Entity<Method>().HasIndex(m => m.CurrentName);
        mb.Entity<Method>().HasIndex(m => m.Status);

        mb.Entity<TrackedFile>()
          .HasIndex(f => new { f.ProjectId, f.RelativePath })
          .IsUnique()
          .HasFilter("[RemovedAt] IS NULL");

        mb.Entity<Language>().HasData(
            new Language { Id = 1, Name = "c",      DisplayName = "C",    Extensions = ".c,.h" },
            new Language { Id = 2, Name = "csharp",  DisplayName = "C#",   Extensions = ".cs" },
            new Language { Id = 3, Name = "java",    DisplayName = "Java", Extensions = ".java" }
        );
    }
}
