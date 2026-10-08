using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace DevCraft.Cli;

public sealed class TerminalAiProjectScanner : IAiProjectScanner
{
    public ProjectScanResult Scan(string directoryPath, string defaultAgent)
    {
        string? controlledResponse = Environment.GetEnvironmentVariable("DEVCRAFT_AI_SCAN_RESPONSE");

        if (!string.IsNullOrWhiteSpace(controlledResponse))
        {
            return ProjectScanJsonParser.Parse(controlledResponse);
        }

        string prompt = ProjectScanPromptBuilder.Build(directoryPath);
        string output = RunAgent(directoryPath, defaultAgent, prompt);

        return ProjectScanJsonParser.Parse(ExtractJsonPayload(output, defaultAgent));
    }

    private static string RunAgent(string directoryPath, string defaultAgent, string prompt)
    {
        ProcessStartInfo startInfo = CreateStartInfo(directoryPath, defaultAgent, prompt);
        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start {defaultAgent}.");

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{defaultAgent} scan failed: {error}");
        }

        return output;
    }

    private static ProcessStartInfo CreateStartInfo(string directoryPath, string defaultAgent, string prompt)
    {
        ProcessStartInfo startInfo = new()
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        if (defaultAgent.Equals("Claude AI", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.FileName = "claude";
            startInfo.ArgumentList.Add("-p");
            startInfo.ArgumentList.Add(prompt);

            return startInfo;
        }

        if (defaultAgent.Equals("GitHub Copilot", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.FileName = "gh";
            startInfo.ArgumentList.Add("copilot");
            startInfo.ArgumentList.Add("-p");
            startInfo.ArgumentList.Add(prompt);

            return startInfo;
        }

        startInfo.FileName = "codex";
        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add("--cd");
        startInfo.ArgumentList.Add(directoryPath);
        startInfo.ArgumentList.Add("--json");
        startInfo.ArgumentList.Add(prompt);

        return startInfo;
    }

    private static string ExtractJsonPayload(string output, string defaultAgent)
    {
        if (!defaultAgent.Equals("Codex", StringComparison.OrdinalIgnoreCase))
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
