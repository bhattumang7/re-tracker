using Microsoft.EntityFrameworkCore;
using Tracker.Api.Services;
using Tracker.Api.Services.Interfaces;
using Tracker.Core.Interfaces;
using Tracker.Data;
using Tracker.Parsers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TrackerDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("ReTracker")));

builder.Services.AddScoped<ISummaryService,  SummaryService>();
builder.Services.AddScoped<IMilestoneService, MilestoneService>();
builder.Services.AddScoped<IMethodService,   MethodService>();
builder.Services.AddScoped<IFileService,     FileService>();
builder.Services.AddScoped<IProjectService,  ProjectService>();
builder.Services.AddScoped<ISearchService,   SearchService>();
builder.Services.AddScoped<IScanService,     ScanService>();
builder.Services.AddSingleton<ScanProgressStore>();

// Language parsers — add new parsers here; no other code needs to change
builder.Services.AddScoped<ILanguageParser, CLanguageParser>();
builder.Services.AddScoped<ILanguageParser, CSharpLanguageParser>();
builder.Services.AddScoped<ILanguageParser, JavaLanguageParser>();

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "re-tracker API", Version = "v1" });
});

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:4200", "http://localhost:4201")
     .AllowAnyHeader()
     .AllowAnyMethod()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "re-tracker v1"));

app.UseCors();
app.MapControllers();

// Auto-apply migrations on startup (dev convenience)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TrackerDbContext>();
    db.Database.Migrate();
}

app.Run();
