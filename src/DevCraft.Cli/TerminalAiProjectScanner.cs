using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace DevCraft.Cli;

public sealed class TerminalAiProjectScanner : IAiProjectScanner
{
    private const int MaximumCapturedOutputLength = 4_000;

    public ProjectScanResult Scan(string directoryPath, string defaultAgent, string profileDirectory)
    {
        string? controlledResponse = Environment.GetEnvironmentVariable("DEVCRAFT_AI_SCAN_RESPONSE");

        if (!string.IsNullOrWhiteSpace(controlledResponse))
        {
            return ProjectScanJsonParser.Parse(controlledResponse);
        }

        string prompt = ProjectScanPromptBuilder.Build(directoryPath);
        SupportedTerminalClient client = ResolveClient(defaultAgent, profileDirectory);
        TerminalProcessResult processResult = RunAgent(directoryPath, defaultAgent, client, prompt);
        TerminalClientOutput clientOutput;

        clientOutput = TerminalClientOutputExtractor.Extract(processResult.StandardOutput, client);

        if (processResult.ExitCode != 0 || clientOutput.Errors.Count > 0)
        {
            throw new InvalidOperationException(BuildFailureMessage(defaultAgent, client, processResult, clientOutput));
        }

        try
        {
            return ProjectScanJsonParser.Parse(clientOutput.Response);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidOperationException(BuildInvalidResponseMessage(defaultAgent, client, processResult, clientOutput, exception), exception);
        }
    }

    private static SupportedTerminalClient ResolveClient(string defaultAgent, string profileDirectory)
    {
        IReadOnlyList<SupportedTerminalClient> clients = ProfileConfigurationReader.Read(profileDirectory).SupportedClients;
        return SupportedTerminalClientResolver.Resolve(defaultAgent, clients.Count == 0 ? SupportedTerminalClientCatalog.Create() : clients);
    }

    private static TerminalProcessResult RunAgent(string directoryPath, string defaultAgent, SupportedTerminalClient client, string prompt)
    {
        ProcessStartInfo startInfo = CreateStartInfo(directoryPath, client, prompt);
        using Process process = StartAgent(defaultAgent, startInfo);

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new TerminalProcessResult(process.ExitCode, output, error);
    }

    private static Process StartAgent(string defaultAgent, ProcessStartInfo startInfo)
    {
        try
        {
            return Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Could not start {defaultAgent}.");
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException(
                BuildStartFailureMessage(defaultAgent, startInfo.FileName, exception.Message),
                exception);
        }
    }

    internal static string BuildStartFailureMessage(string defaultAgent, string binaryPath, string exceptionMessage)
    {
        return $"{defaultAgent} scan could not start '{binaryPath}'. Confirm the CLI is installed and available on PATH. {exceptionMessage}";
    }

    internal static ProcessStartInfo CreateStartInfo(string directoryPath, SupportedTerminalClient client, string prompt)
    {
        ProcessStartInfo startInfo = new()
        {
            WorkingDirectory = directoryPath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        startInfo.FileName = client.Scan.BinaryPath;

        foreach (string argument in client.Scan.Arguments)
        {
            startInfo.ArgumentList.Add(argument
                .Replace("{workingDirectory}", directoryPath, StringComparison.Ordinal)
                .Replace("{prompt}", prompt, StringComparison.Ordinal));
        }

        return startInfo;
    }

    internal static string BuildFailureMessage(string requestedAgent, SupportedTerminalClient client, TerminalProcessResult processResult, TerminalClientOutput clientOutput)
    {
        StringBuilder message = new($"{client.Name} scan failed with exit code {processResult.ExitCode}.");
        AppendOutput(message, "Requested agent", requestedAgent);
        AppendOutput(message, "Executable", client.Scan.BinaryPath);
        AppendOutput(message, "Provider error", string.Join(Environment.NewLine, clientOutput.Errors));
        AppendOutput(message, "stderr", processResult.StandardError);
        AppendOutput(message, "stdout", clientOutput.Response);
        AppendOutput(message, "Provider warning", string.Join(Environment.NewLine, clientOutput.Warnings));
        AppendHint(message, client, clientOutput, processResult);

        if (processResult.StandardError.Contains("failed to load models cache", StringComparison.OrdinalIgnoreCase) &&
            processResult.StandardError.Contains("base_instructions", StringComparison.OrdinalIgnoreCase))
        {
            message.AppendLine();
            message.Append("Codex also reported a model cache schema warning. That warning is normally recoverable in current Codex versions; update Codex and refresh only the model cache if it persists.");
        }

        return message.ToString();
    }

    internal static string BuildInvalidResponseMessage(
        string requestedAgent,
        SupportedTerminalClient client,
        TerminalProcessResult processResult,
        TerminalClientOutput clientOutput,
        Exception parseException)
    {
        StringBuilder message = new($"{client.Name} scan finished, but DevCraft could not read project scan JSON.");
        AppendOutput(message, "Requested agent", requestedAgent);
        AppendOutput(message, "Executable", client.Scan.BinaryPath);
        AppendOutput(message, "Parse error", parseException.Message);
        AppendOutput(message, "Provider error", string.Join(Environment.NewLine, clientOutput.Errors));
        AppendOutput(message, "stdout", clientOutput.Response);
        AppendOutput(message, "stderr", processResult.StandardError);
        AppendOutput(message, "Provider warning", string.Join(Environment.NewLine, clientOutput.Warnings));
        AppendHint(message, client, clientOutput, processResult);

        return message.ToString();
    }

    private static void AppendOutput(StringBuilder message, string label, string output)
    {
        string trimmed = BoundAndRedact(output);

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return;
        }

        message.AppendLine();
        message.Append(label);
        message.Append(": ");
        message.Append(trimmed);
    }

    private static void AppendHint(StringBuilder message, SupportedTerminalClient client, TerminalClientOutput clientOutput, TerminalProcessResult processResult)
    {
        string diagnosticText = string.Join(
            Environment.NewLine,
            clientOutput.Errors.Concat(clientOutput.Warnings).Concat([processResult.StandardError, clientOutput.Response]));

        if (!client.Slug.Equals("codex", StringComparison.OrdinalIgnoreCase) &&
            diagnosticText.Contains("\"thread.started\"", StringComparison.OrdinalIgnoreCase) &&
            diagnosticText.Contains("not supported when using Codex", StringComparison.OrdinalIgnoreCase))
        {
            message.AppendLine();
            message.Append("Hint: The selected client is not Codex, but the captured output looks like Codex JSON events. Check the configured executable for this client in the profile configure.json file.");
            return;
        }

        if (diagnosticText.Contains("not supported when using Codex with a ChatGPT account", StringComparison.OrdinalIgnoreCase))
        {
            message.AppendLine();
            message.Append("Hint: Codex rejected the configured model for the current ChatGPT login. Change the Codex model in the Codex configuration or log in with an account that supports that model, then run DevCraft again.");
            return;
        }

        if (diagnosticText.Contains("login", StringComparison.OrdinalIgnoreCase) ||
            diagnosticText.Contains("authentication", StringComparison.OrdinalIgnoreCase) ||
            diagnosticText.Contains("unauthorized", StringComparison.OrdinalIgnoreCase))
        {
            message.AppendLine();
            message.Append("Hint: The terminal AI client appears to need login or authentication repair. Run the client directly and complete its login flow before retrying DevCraft.");
        }

    }

    private static string BoundAndRedact(string value)
    {
        string trimmed = value.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        string redacted = Regex.Replace(trimmed, "sk-[A-Za-z0-9_-]{12,}", "sk-REDACTED");
        redacted = Regex.Replace(redacted, "(Bearer\\s+)[A-Za-z0-9._~+/-]+=*", "$1REDACTED", RegexOptions.IgnoreCase);

        return redacted.Length <= MaximumCapturedOutputLength
            ? redacted
            : $"{redacted[..MaximumCapturedOutputLength]}...";
    }
}
