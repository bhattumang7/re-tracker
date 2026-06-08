using System.CommandLine;
using System.Net.Http.Json;
using Tracker.Cli.Infrastructure;

namespace Tracker.Cli.Commands;

public static class ScanCommand
{
    public static Command Build()
    {
        var projectOpt = new Option<int>("--project") { Description = "Project ID to scan", Required = true };

        var cmd = new Command("scan", "Trigger a project scan via the API");
        cmd.Add(projectOpt);

        cmd.SetAction(async result =>
        {
            var project = result.GetValue(projectOpt);
            var apiBase = Environment.GetEnvironmentVariable("RETRACKER_API") ?? "http://localhost:5000";
            using var http = new HttpClient { BaseAddress = new Uri(apiBase) };

            Console.WriteLine($"Triggering scan for project {project}...");
            var response = await http.PostAsync($"/api/projects/{project}/scan", null);

            if (!response.IsSuccessStatusCode)
            {
                OutputFormatter.PrintError($"Scan failed: {response.StatusCode}");
                return;
            }

            var job = await response.Content.ReadFromJsonAsync<JobResponse>();
            Console.WriteLine($"Job ID: {job?.JobId}");

            while (true)
            {
                await Task.Delay(2000);
                var statusResp = await http.GetAsync($"/api/projects/{project}/scan/status?jobId={job?.JobId}");
                if (!statusResp.IsSuccessStatusCode) break;
                var status = await statusResp.Content.ReadFromJsonAsync<ScanStatusResponse>();
                Console.Write($"\r  {status?.Processed}/{status?.Total} files");
                if (status?.Complete == true)
                {
                    Console.WriteLine(status.Error is null ? "\nDone." : $"\nError: {status.Error}");
                    break;
                }
            }
        });

        return cmd;
    }

    private record JobResponse(Guid JobId);
    private record ScanStatusResponse(int Total, int Processed, bool Complete, string? Error);
}
