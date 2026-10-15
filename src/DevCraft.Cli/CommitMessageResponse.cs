using System.Text.Json;

namespace DevCraft.Cli;

public sealed record CommitMessageResponse(bool Success, string Summary, string Message)
{
    public static CommitMessageResponse Parse(string response)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(JsonPayloadExtractor.Extract(response));
            JsonElement root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return Failed("The agent did not return a JSON object.");
            }

            bool success = root.TryGetProperty("success", out JsonElement successElement) && successElement.ValueKind == JsonValueKind.True;
            string summary = ReadString(root, "summary");
            string message = ReadString(root, "message");

            if (!success)
            {
                return Failed(summary.Length > 0 ? summary : "The agent could not write a commit message.");
            }

            return message.Length == 0
                ? Failed("The agent returned an empty commit message.")
                : new CommitMessageResponse(true, summary, message);
        }
        catch (JsonException)
        {
            return Failed("The agent did not return valid JSON.");
        }
    }

    private static CommitMessageResponse Failed(string summary)
    {
        return new CommitMessageResponse(false, summary, string.Empty);
    }

    private static string ReadString(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out JsonElement element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()?.Trim() ?? string.Empty
            : string.Empty;
    }
}
