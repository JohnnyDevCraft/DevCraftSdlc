using System.Text.Json;

namespace DevCraft.Cli;

public static class ProjectScanJsonParser
{
    public static ProjectScanResult Parse(string json)
    {
        string payload = ExtractJson(json);
        ProjectScanJson? result;

        try
        {
            result = JsonSerializer.Deserialize<ProjectScanJson>(
                payload,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("AI scan did not return valid project scan JSON.", exception);
        }

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

    private static string ExtractJson(string value)
    {
        string trimmed = value.Trim();

        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            trimmed = StripCodeFence(trimmed);
        }

        int objectStart = trimmed.IndexOf('{');
        int arrayStart = trimmed.IndexOf('[');
        int start = FirstJsonStart(objectStart, arrayStart);

        if (start > 0)
        {
            trimmed = trimmed[start..];
        }

        int objectEnd = trimmed.LastIndexOf('}');
        int arrayEnd = trimmed.LastIndexOf(']');
        int end = Math.Max(objectEnd, arrayEnd);

        return end >= 0
            ? trimmed[..(end + 1)]
            : trimmed;
    }

    private static string StripCodeFence(string value)
    {
        string withoutOpeningFence = value;
        int firstNewLine = value.IndexOf('\n');

        if (firstNewLine >= 0)
        {
            withoutOpeningFence = value[(firstNewLine + 1)..];
        }

        int closingFence = withoutOpeningFence.LastIndexOf("```", StringComparison.Ordinal);

        return closingFence >= 0
            ? withoutOpeningFence[..closingFence].Trim()
            : withoutOpeningFence.Trim();
    }

    private static int FirstJsonStart(int objectStart, int arrayStart)
    {
        if (objectStart < 0)
        {
            return arrayStart;
        }

        if (arrayStart < 0)
        {
            return objectStart;
        }

        return Math.Min(objectStart, arrayStart);
    }
}
