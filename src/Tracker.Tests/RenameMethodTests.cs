using Microsoft.EntityFrameworkCore;
using Tracker.Api.Services;
using Tracker.Data;
using Tracker.Data.Entities;

namespace Tracker.Tests;

public class RenameMethodTests
{
    private static TrackerDbContext MakeDb() =>
        new(new DbContextOptionsBuilder<TrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<(TrackerDbContext db, Method method)> SeedAsync()
    {
        var db   = MakeDb();
        var file = new TrackedFile { RelativePath = "test.c", ProjectId = 1, LastScannedAt = DateTime.UtcNow };
        db.Files.Add(file);
        await db.SaveChangesAsync();

        var method = new Method
        {
            FileId      = file.Id,
            CurrentName = "sub_1234",
            OriginalName = "sub_1234",
            ReturnType  = "int",
            StartLine   = 10,
            StartColumn = 0,
            CreatedAt   = DateTime.UtcNow,
            UpdatedAt   = DateTime.UtcNow
        };
        db.Methods.Add(method);
        await db.SaveChangesAsync();
        return (db, method);
    }

    [Fact]
    public async Task Rename_UpdatesCurrentName()
    {
        var (db, method) = await SeedAsync();
        var svc = new MethodService(db);

        await svc.RenameAsync(method.Id, "processPacket", null);

        var updated = await db.Methods.FindAsync(method.Id);
        Assert.Equal("processPacket", updated!.CurrentName);
    }

    [Fact]
    public async Task Rename_PreservesOriginalName()
    {
        var (db, method) = await SeedAsync();
        var svc = new MethodService(db);

        await svc.RenameAsync(method.Id, "processPacket", null);

        var updated = await db.Methods.FindAsync(method.Id);
        Assert.Equal("sub_1234", updated!.OriginalName);
    }

    [Fact]
    public async Task Rename_WritesRenameHistoryEntry()
    {
        var (db, method) = await SeedAsync();
        var svc = new MethodService(db);

        await svc.RenameAsync(method.Id, "processPacket", null);

        var history = await db.RenameHistories.SingleAsync(h => h.MethodId == method.Id);
        Assert.Equal("sub_1234",      history.OldName);
        Assert.Equal("processPacket", history.NewName);
        Assert.Equal("Method",        history.EntityType);
    }

    [Fact]
    public async Task Rename_StoresComment()
    {
        var (db, method) = await SeedAsync();
        var svc = new MethodService(db);

        await svc.RenameAsync(method.Id, "processPacket", "renamed during packet analysis");

        var history = await db.RenameHistories.SingleAsync(h => h.MethodId == method.Id);
        Assert.Equal("renamed during packet analysis", history.Comment);
    }

    [Fact]
    public async Task Rename_ReturnsDto_WithNewName()
    {
        var (db, method) = await SeedAsync();
        var svc = new MethodService(db);

        var result = await svc.RenameAsync(method.Id, "processPacket", null);

        Assert.NotNull(result);
        Assert.Equal("processPacket", result.CurrentName);
        Assert.Equal("sub_1234",      result.OriginalName);
    }

    [Fact]
    public async Task Rename_ReturnsNull_ForMissingId()
    {
        var (db, _) = await SeedAsync();
        var svc = new MethodService(db);

        var result = await svc.RenameAsync(999, "processPacket", null);

        Assert.Null(result);
    }

    [Fact]
    public async Task Rename_ReturnsNull_ForSoftDeletedMethod()
    {
        var (db, method) = await SeedAsync();
        method.RemovedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var svc = new MethodService(db);
        var result = await svc.RenameAsync(method.Id, "processPacket", null);

        Assert.Null(result);
    }
}
