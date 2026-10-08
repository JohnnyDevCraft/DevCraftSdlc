using System.Text.Json;

namespace DevCraft.Cli;

public static class ProjectScanJsonParser
{
    public static ProjectScanResult Parse(string json)
    {
        ProjectScanJson? result = JsonSerializer.Deserialize<ProjectScanJson>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

        if (result is null)
        {
            throw new InvalidOperationException("AI scan returned empty JSON.");
        }

        List<DetectedProject> projects = result.Projects
            .Select(project => new DetectedProject(project.Name, project.Type))
            .ToList();

        ProjectProfile profile = new(
            Clean(result.ProjectProfile?.Name, "Unknown Project"),
            Clean(result.ProjectProfile?.Purpose, "Pending."),
            Clean(result.ProjectProfile?.Description, "Pending."));

        AiSdlcDetection aiDrivenSdlc = new(
            result.AiDrivenSdlc?.Detected ?? false,
            result.AiDrivenSdlc?.Name);

        return new ProjectScanResult(profile, projects, aiDrivenSdlc);
    }

    private static string Clean(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
