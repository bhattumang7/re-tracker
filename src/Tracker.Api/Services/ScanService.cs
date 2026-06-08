using Microsoft.EntityFrameworkCore;
using Tracker.Api.Services.Interfaces;
using Tracker.Core.DTOs;
using Tracker.Core.Interfaces;
using Tracker.Data;
using Tracker.Data.Entities;

namespace Tracker.Api.Services;

public class ScanService(
    TrackerDbContext db,
    IEnumerable<ILanguageParser> parsers,
    ScanProgressStore progress) : IScanService
{
    public async Task<ScanJobDto> TriggerScanAsync(int projectId)
    {
        var project = await db.Projects.Include(p => p.Language).FirstOrDefaultAsync(p => p.Id == projectId)
            ?? throw new ArgumentException($"Project {projectId} not found");

        var jobId = progress.Create();

        // Capture connection string before the request scope ends and db is disposed.
        var connectionString = db.Database.GetConnectionString()!;

        _ = Task.Run(async () =>
        {
            try
            {
                await RunScanAsync(project, jobId, connectionString);
            }
            catch (Exception ex)
            {
                progress.Update(jobId, 0, 0, true, ex.Message);
            }
        });

        return new ScanJobDto(jobId);
    }

    public ScanStatusDto? GetStatus(Guid jobId) => progress.Get(jobId);

    private async Task RunScanAsync(Project project, Guid jobId, string connectionString)
    {
        var parser = parsers.FirstOrDefault(p => p.LanguageName == project.Language.Name)
            ?? throw new InvalidOperationException($"No parser for language '{project.Language.Name}'");

        var extensions = project.Language.Extensions
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(project.RootPath))
            throw new DirectoryNotFoundException($"Root path not found: {project.RootPath}");

        var files = Directory.EnumerateFiles(project.RootPath, "*", SearchOption.AllDirectories)
            .Where(f => extensions.Contains(Path.GetExtension(f)))
            .ToList();

        progress.Update(jobId, files.Count, 0, false);

        // Use a fresh DbContext per scan to avoid concurrency issues with the request context
        var optionsBuilder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<TrackerDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        await using var scanDb = new TrackerDbContext(optionsBuilder.Options);

        for (int i = 0; i < files.Count; i++)
        {
            var filePath     = files[i];
            var relativePath = Path.GetRelativePath(project.RootPath, filePath).Replace('\\', '/');
            var content      = await File.ReadAllTextAsync(filePath);
            var parseResult  = parser.Parse(filePath, content);

            await ReconcileFileAsync(scanDb, project.Id, relativePath, parseResult);
            progress.Update(jobId, files.Count, i + 1, false);
        }

        project.LastScannedAt = DateTime.UtcNow;
        await scanDb.Projects.Where(p => p.Id == project.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastScannedAt, DateTime.UtcNow));

        progress.Update(jobId, files.Count, files.Count, true);
    }

    private static async Task ReconcileFileAsync(
        TrackerDbContext scanDb, int projectId, string relativePath, Core.Models.ParseResult parseResult)
    {
        var trackedFile = await scanDb.Files
            .FirstOrDefaultAsync(f => f.ProjectId == projectId && f.RelativePath == relativePath && f.RemovedAt == null);

        if (trackedFile is null)
        {
            trackedFile = new TrackedFile
            {
                ProjectId    = projectId,
                RelativePath = relativePath,
                LastScannedAt = DateTime.UtcNow
            };
            scanDb.Files.Add(trackedFile);
            await scanDb.SaveChangesAsync();
        }
        else
        {
            trackedFile.LastScannedAt = DateTime.UtcNow;
        }

        var existingMethods = await scanDb.Methods
            .Where(m => m.FileId == trackedFile.Id && m.RemovedAt == null)
            .Include(m => m.Parameters)
            .ToListAsync();

        var matchedIds = new HashSet<int>();

        foreach (var parsed in parseResult.Methods)
        {
            var existing = MethodMatcher.FindMatch(existingMethods, parsed, matchedIds);

            if (existing is null)
            {
                var method = new Method
                {
                    FileId          = trackedFile.Id,
                    CurrentName     = parsed.Name,
                    OriginalName    = parsed.Name,
                    ReturnType      = parsed.ReturnType,
                    StartLine       = parsed.StartLine,
                    StartColumn     = parsed.StartColumn,
                    EndLine         = parsed.EndLine,
                    EndColumn       = parsed.EndColumn,
                    BodyStartLine   = parsed.BodyStartLine,
                    BodyStartColumn = parsed.BodyStartColumn,
                    BodyEndLine     = parsed.BodyEndLine,
                    BodyEndColumn   = parsed.BodyEndColumn,
                    CreatedAt       = DateTime.UtcNow,
                    UpdatedAt       = DateTime.UtcNow
                };
                scanDb.Methods.Add(method);
                await scanDb.SaveChangesAsync();
                matchedIds.Add(method.Id);

                foreach (var (p, idx) in parsed.Parameters.Select((p, i) => (p, i)))
                {
                    scanDb.MethodParameters.Add(new MethodParameter
                    {
                        MethodId     = method.Id,
                        CurrentName  = p.Name,
                        OriginalName = p.Name,
                        Type         = p.Type,
                        Ordinal      = idx,
                        StartLine    = p.StartLine,
                        StartColumn  = p.StartColumn,
                        EndLine      = p.EndLine,
                        EndColumn    = p.EndColumn
                    });
                }
            }
            else
            {
                matchedIds.Add(existing.Id);

                bool posChanged =
                    existing.StartLine != parsed.StartLine || existing.EndLine != parsed.EndLine;

                if (posChanged)
                {
                    scanDb.RenameHistories.Add(new RenameHistory
                    {
                        MethodId       = existing.Id,
                        EntityType     = "Method",
                        OldName        = existing.CurrentName,
                        NewName        = existing.CurrentName,
                        OldStartLine   = existing.StartLine,
                        OldStartColumn = existing.StartColumn,
                        NewStartLine   = parsed.StartLine,
                        NewStartColumn = parsed.StartColumn,
                        Timestamp      = DateTime.UtcNow,
                        Comment        = "Line position updated by rescan"
                    });
                }

                existing.StartLine       = parsed.StartLine;
                existing.StartColumn     = parsed.StartColumn;
                existing.EndLine         = parsed.EndLine;
                existing.EndColumn       = parsed.EndColumn;
                existing.BodyStartLine   = parsed.BodyStartLine;
                existing.BodyStartColumn = parsed.BodyStartColumn;
                existing.BodyEndLine     = parsed.BodyEndLine;
                existing.BodyEndColumn   = parsed.BodyEndColumn;
                existing.UpdatedAt       = DateTime.UtcNow;

                // Update parameter positions (matched by ordinal)
                foreach (var pp in parsed.Parameters)
                {
                    var ep = existing.Parameters.FirstOrDefault(p => p.Ordinal == pp.Ordinal);
                    if (ep is null) continue;
                    ep.StartLine   = pp.StartLine;
                    ep.StartColumn = pp.StartColumn;
                    ep.EndLine     = pp.EndLine;
                    ep.EndColumn   = pp.EndColumn;
                }
            }
        }

        // Soft-delete methods that could not be matched to any parsed symbol
        foreach (var existing in existingMethods.Where(e => !matchedIds.Contains(e.Id)))
            existing.RemovedAt = DateTime.UtcNow;

        await scanDb.SaveChangesAsync();
    }

}
