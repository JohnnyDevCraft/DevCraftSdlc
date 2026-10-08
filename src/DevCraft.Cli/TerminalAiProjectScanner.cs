using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace DevCraft.Cli;

public sealed class TerminalAiProjectScanner : IAiProjectScanner
{
    private const int MaximumCapturedOutputLength = 4_000;

    public ProjectScanResult Scan(string directoryPath, string defaultAgent)
    {
        string? controlledResponse = Environment.GetEnvironmentVariable("DEVCRAFT_AI_SCAN_RESPONSE");

        if (!string.IsNullOrWhiteSpace(controlledResponse))
        {
            return ProjectScanJsonParser.Parse(controlledResponse);
        }

        string prompt = ProjectScanPromptBuilder.Build(directoryPath);
        TerminalProcessResult processResult = RunAgent(directoryPath, defaultAgent, prompt);
        TerminalClientOutput clientOutput;

        SupportedTerminalClient client = SupportedTerminalClientCatalog
            .Create()
            .FirstOrDefault(client => client.Name.Equals(defaultAgent, StringComparison.OrdinalIgnoreCase))
            ?? SupportedTerminalClientCatalog.Create()[0];

        clientOutput = TerminalClientOutputExtractor.Extract(processResult.StandardOutput, client);

        if (processResult.ExitCode != 0 || clientOutput.Errors.Count > 0)
        {
            throw new InvalidOperationException(BuildFailureMessage(defaultAgent, processResult, clientOutput));
        }

        try
        {
            return ProjectScanJsonParser.Parse(clientOutput.Response);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidOperationException(BuildInvalidResponseMessage(defaultAgent, processResult, clientOutput, exception), exception);
        }
    }

    private static TerminalProcessResult RunAgent(string directoryPath, string defaultAgent, string prompt)
    {
        ProcessStartInfo startInfo = CreateStartInfo(directoryPath, defaultAgent, prompt);
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

    private static ProcessStartInfo CreateStartInfo(string directoryPath, string defaultAgent, string prompt)
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

        if (defaultAgent.Equals("Claude AI", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.FileName = "claude";
            startInfo.ArgumentList.Add("--print");
            startInfo.ArgumentList.Add("--output-format");
            startInfo.ArgumentList.Add("json");
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
        startInfo.ArgumentList.Add("--skip-git-repo-check");
        startInfo.ArgumentList.Add(prompt);

        return startInfo;
    }

    internal static string BuildFailureMessage(string defaultAgent, TerminalProcessResult processResult, TerminalClientOutput clientOutput)
    {
        StringBuilder message = new($"{defaultAgent} scan failed with exit code {processResult.ExitCode}.");
        AppendOutput(message, "Provider error", string.Join(Environment.NewLine, clientOutput.Errors));
        AppendOutput(message, "stderr", processResult.StandardError);
        AppendOutput(message, "stdout", clientOutput.Response);
        AppendOutput(message, "Provider warning", string.Join(Environment.NewLine, clientOutput.Warnings));
        AppendHint(message, clientOutput, processResult);

        if (processResult.StandardError.Contains("failed to load models cache", StringComparison.OrdinalIgnoreCase) &&
            processResult.StandardError.Contains("base_instructions", StringComparison.OrdinalIgnoreCase))
        {
            message.AppendLine();
            message.Append("Codex also reported a model cache schema warning. That warning is normally recoverable in current Codex versions; update Codex and refresh only the model cache if it persists.");
        }

        return message.ToString();
    }

    internal static string BuildInvalidResponseMessage(
        string defaultAgent,
        TerminalProcessResult processResult,
        TerminalClientOutput clientOutput,
        Exception parseException)
    {
        StringBuilder message = new($"{defaultAgent} scan finished, but DevCraft could not read project scan JSON.");
        AppendOutput(message, "Parse error", parseException.Message);
        AppendOutput(message, "Provider error", string.Join(Environment.NewLine, clientOutput.Errors));
        AppendOutput(message, "stdout", clientOutput.Response);
        AppendOutput(message, "stderr", processResult.StandardError);
        AppendOutput(message, "Provider warning", string.Join(Environment.NewLine, clientOutput.Warnings));
        AppendHint(message, clientOutput, processResult);

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

    private static void AppendHint(StringBuilder message, TerminalClientOutput clientOutput, TerminalProcessResult processResult)
    {
        string diagnosticText = string.Join(
            Environment.NewLine,
            clientOutput.Errors.Concat(clientOutput.Warnings).Concat([processResult.StandardError, clientOutput.Response]));

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
