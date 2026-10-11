using System.Diagnostics;
using System.Text.Json;

namespace DevCraft.Cli;

public sealed class TerminalDevCraftRepositoryUpgradeLauncher : IDevCraftRepositoryUpgradeLauncher
{
    public DevCraftRepositoryUpgradeResult Upgrade(DevCraftRepositoryUpgradeRequest request)
    {
        SupportedTerminalClient? client = SelectClient(request.Context.ProfileDirectory);

        if (client is null)
        {
            return DevCraftRepositoryUpgradeResult.Failed("No supported terminal agent is configured for DevCraft repository upgrade.");
        }

        string prompt = DevCraftRepositoryUpgradePromptBuilder.Build(request);
        ProcessStartInfo startInfo = new()
        {
            FileName = client.Scan.BinaryPath,
            WorkingDirectory = request.Context.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (string argument in client.Scan.Arguments)
        {
            startInfo.ArgumentList.Add(argument
                .Replace("{workingDirectory}", request.Context.CurrentDirectory, StringComparison.Ordinal)
                .Replace("{prompt}", prompt, StringComparison.Ordinal));
        }

        try
        {
            using Process process = Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Could not start {client.Name}.");
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                return DevCraftRepositoryUpgradeResult.Failed($"Upgrade agent failed: {error.Trim()}");
            }

            return ParseResult(output);
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return DevCraftRepositoryUpgradeResult.Failed($"Upgrade agent failed: {exception.Message}");
        }
    }

    private static SupportedTerminalClient? SelectClient(string profileDirectory)
    {
        DevCraftProfileConfiguration configuration = ProfileConfigurationReader.Read(profileDirectory);

        return configuration.SupportedClients.FirstOrDefault()
            ?? SupportedTerminalClientCatalog.Create().FirstOrDefault();
    }

    private static DevCraftRepositoryUpgradeResult ParseResult(string output)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(output);
            JsonElement root = document.RootElement;
            bool success = root.TryGetProperty("success", out JsonElement successElement) &&
                successElement.ValueKind == JsonValueKind.True;
            string summary = root.TryGetProperty("summary", out JsonElement summaryElement) &&
                summaryElement.ValueKind == JsonValueKind.String
                    ? summaryElement.GetString() ?? string.Empty
                    : string.Empty;

            return new DevCraftRepositoryUpgradeResult(
                success,
                summary,
                ReadArray(root, "movedFeatures"),
                ReadArray(root, "skippedFeatures"));
        }
        catch (JsonException)
        {
            return DevCraftRepositoryUpgradeResult.Failed("Upgrade agent did not return valid JSON.");
        }
    }

    private static IReadOnlyList<string> ReadArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement element) ||
            element.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return element.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();
    }
}
