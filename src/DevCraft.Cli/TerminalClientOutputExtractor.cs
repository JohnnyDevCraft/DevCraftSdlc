using System.Text.Json;

namespace DevCraft.Cli;

public static class TerminalClientOutputExtractor
{
    private const int MaximumDiagnosticLength = 2_000;

    public static string ExtractResponse(string output, SupportedTerminalClient client)
    {
        return Extract(output, client).Response;
    }

    public static TerminalClientOutput Extract(string output, SupportedTerminalClient client)
    {
        if (!client.Slug.Equals("codex", StringComparison.OrdinalIgnoreCase))
        {
            return ExtractStructuredOrPlainText(output);
        }

        IReadOnlyList<CodexLineResult> results = output
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(TryReadAgentMessage)
            .Where(result => result is not null)
            .Cast<CodexLineResult>()
            .ToList();

        string? agentMessage = results
            .Where(result => result.Kind == CodexLineKind.AgentMessage)
            .Select(result => result.Message)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .LastOrDefault();

        List<string> errors = results
            .Where(result => result.Kind == CodexLineKind.Error)
            .Select(result => result.Message)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Select(Bound)
            .ToList();

        List<string> warnings = results
            .Where(result => result.Kind == CodexLineKind.Warning)
            .Select(result => result.Message)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Select(Bound)
            .ToList();

        return new TerminalClientOutput(agentMessage ?? output, errors, warnings);
    }

    private static TerminalClientOutput ExtractStructuredOrPlainText(string output)
    {
        string trimmed = output.Trim();

        if (string.IsNullOrWhiteSpace(trimmed) || !trimmed.StartsWith("{", StringComparison.Ordinal))
        {
            return new TerminalClientOutput(output, [], []);
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(trimmed);
            JsonElement root = document.RootElement;
            string? response = ReadString(root, "result") ??
                ReadString(root, "response") ??
                ReadString(root, "message") ??
                ReadString(root, "text") ??
                output;
            string? error = ReadNestedString(root, "error", "message") ??
                ReadString(root, "error") ??
                ReadString(root, "message");
            string? type = ReadString(root, "type");
            IReadOnlyList<string> errors = IsErrorLike(type, error)
                ? [Bound(error!)]
                : [];

            return new TerminalClientOutput(response, errors, []);
        }
        catch (JsonException)
        {
            return new TerminalClientOutput(output, [], []);
        }
    }

    private static CodexLineResult? TryReadAgentMessage(string line)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(line);
            JsonElement root = document.RootElement;

            string? rootType = ReadString(root, "type");

            if (rootType == "item.completed" &&
                root.TryGetProperty("item", out JsonElement itemElement))
            {
                string? itemType = ReadString(itemElement, "type");

                if (itemType == "agent_message")
                {
                    string? text = ReadString(itemElement, "text");

                    return string.IsNullOrWhiteSpace(text)
                        ? null
                        : new CodexLineResult(CodexLineKind.AgentMessage, text);
                }

                string? itemMessage = ReadString(itemElement, "message") ??
                    ReadString(itemElement, "text") ??
                    ReadNestedString(itemElement, "error", "message");

                if (!string.IsNullOrWhiteSpace(itemMessage))
                {
                    return new CodexLineResult(Classify(itemMessage), itemMessage);
                }
            }

            string? message = ReadNestedString(root, "error", "message") ??
                ReadString(root, "message") ??
                ReadString(root, "error");

            if (!string.IsNullOrWhiteSpace(message) &&
                (rootType == "error" || rootType == "turn.failed" || IsErrorLike(rootType, message)))
            {
                return new CodexLineResult(Classify(message), message);
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static CodexLineKind Classify(string message)
    {
        return IsRecoverableWarning(message)
            ? CodexLineKind.Warning
            : CodexLineKind.Error;
    }

    private static bool IsErrorLike(string? type, string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(type) &&
            (type.Contains("error", StringComparison.OrdinalIgnoreCase) ||
             type.Contains("failed", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsRecoverableWarning(string message)
    {
        return message.Contains("failed to load models cache", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Ignoring unknown feature", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : property.GetRawText();
    }

    private static string? ReadNestedString(JsonElement element, string propertyName, string childPropertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property) ||
            property.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return ReadString(property, childPropertyName);
    }

    private static string Bound(string value)
    {
        string trimmed = value.Trim();

        return trimmed.Length <= MaximumDiagnosticLength
            ? trimmed
            : $"{trimmed[..MaximumDiagnosticLength]}...";
    }
}
