using System.Text.Json;

namespace DevCraft.Cli;

// Applies an agent's JSON document response to the repository .devcraft folder.
// Only exact allow-listed file names are written, and only when their content changes.
public static class RepositoryDocumentSync
{
    public static RepositoryDocumentSyncResult Apply(string response, string devcraftDirectory, IReadOnlyList<string> allowedFiles)
    {
        List<(string Name, string Content)> files = [];
        bool success;
        string summary;

        try
        {
            using JsonDocument document = JsonDocument.Parse(JsonPayloadExtractor.Extract(response));
            JsonElement root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return RepositoryDocumentSyncResult.Failed("The agent did not return a JSON object.");
            }

            success = root.TryGetProperty("success", out JsonElement successElement) &&
                successElement.ValueKind == JsonValueKind.True;
            summary = root.TryGetProperty("summary", out JsonElement summaryElement) &&
                summaryElement.ValueKind == JsonValueKind.String
                    ? summaryElement.GetString()?.Trim() ?? string.Empty
                    : string.Empty;

            if (root.TryGetProperty("files", out JsonElement filesElement) && filesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement file in filesElement.EnumerateArray())
                {
                    if (file.ValueKind == JsonValueKind.Object &&
                        file.TryGetProperty("name", out JsonElement name) && name.ValueKind == JsonValueKind.String &&
                        file.TryGetProperty("content", out JsonElement content) && content.ValueKind == JsonValueKind.String)
                    {
                        files.Add((name.GetString() ?? string.Empty, content.GetString() ?? string.Empty));
                    }
                }
            }
        }
        catch (JsonException)
        {
            return RepositoryDocumentSyncResult.Failed("The agent did not return valid JSON.");
        }

        if (!success)
        {
            return RepositoryDocumentSyncResult.Failed(summary.Length > 0 ? summary : "The agent reported that it could not complete the update.");
        }

        List<string> updated = [];
        List<string> skipped = [];

        foreach ((string name, string content) in files)
        {
            string? allowedName = allowedFiles.FirstOrDefault(allowed => allowed.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));

            if (allowedName is null)
            {
                skipped.Add(name);
                continue;
            }

            string path = Path.Combine(devcraftDirectory, allowedName);

            if (File.Exists(path) && Normalize(File.ReadAllText(path)) == Normalize(content))
            {
                continue;
            }

            Directory.CreateDirectory(devcraftDirectory);
            File.WriteAllText(path, content);

            if (!updated.Contains(allowedName))
            {
                updated.Add(allowedName);
            }
        }

        return new RepositoryDocumentSyncResult(true, summary, updated, skipped);
    }

    private static string Normalize(string value)
    {
        return value.ReplaceLineEndings("\n").TrimEnd();
    }
}
