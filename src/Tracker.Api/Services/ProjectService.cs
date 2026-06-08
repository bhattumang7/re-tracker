using Microsoft.EntityFrameworkCore;
using Tracker.Api.Services.Interfaces;
using Tracker.Core.DTOs;
using Tracker.Data;
using Tracker.Data.Entities;

namespace Tracker.Api.Services;

public class ProjectService(TrackerDbContext db) : IProjectService
{
    public async Task<List<ProjectDto>> ListAsync()
        => await db.Projects
            .Include(p => p.Language)
            .Select(p => new ProjectDto(p.Id, p.Name, p.RootPath, p.Language.DisplayName, p.LastScannedAt))
            .ToListAsync();

    public async Task<ProjectDto?> GetAsync(int id)
        => await db.Projects
            .Include(p => p.Language)
            .Where(p => p.Id == id)
            .Select(p => new ProjectDto(p.Id, p.Name, p.RootPath, p.Language.DisplayName, p.LastScannedAt))
            .FirstOrDefaultAsync();

    public async Task<ProjectDto> CreateAsync(string name, string rootPath, int languageId)
    {
        var lang = await db.Languages.FindAsync(languageId)
            ?? throw new ArgumentException($"Language {languageId} not found");

        var project = new Project
        {
            Name       = name,
            RootPath   = rootPath,
            LanguageId = languageId,
            CreatedAt  = DateTime.UtcNow
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return new ProjectDto(project.Id, project.Name, project.RootPath, lang.DisplayName, null);
    }
}
