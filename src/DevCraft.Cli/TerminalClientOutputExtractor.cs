using System.Text.Json;

namespace DevCraft.Cli;

public static class TerminalClientOutputExtractor
{
    public static string ExtractResponse(string output, SupportedTerminalClient client)
    {
        if (!client.Slug.Equals("codex", StringComparison.OrdinalIgnoreCase))
        {
            return output;
        }

        string? agentMessage = output
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(TryReadAgentMessage)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .LastOrDefault();

        return agentMessage ?? output;
    }

    private static string? TryReadAgentMessage(string line)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(line);
            JsonElement root = document.RootElement;

            if (!root.TryGetProperty("type", out JsonElement typeElement) ||
                typeElement.GetString() != "item.completed" ||
                !root.TryGetProperty("item", out JsonElement itemElement) ||
                !itemElement.TryGetProperty("type", out JsonElement itemTypeElement) ||
                itemTypeElement.GetString() != "agent_message" ||
                !itemElement.TryGetProperty("text", out JsonElement textElement))
            {
                return null;
            }

            return textElement.GetString();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
