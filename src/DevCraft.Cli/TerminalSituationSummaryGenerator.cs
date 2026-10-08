using System.Diagnostics;
using System.Text;

namespace DevCraft.Cli;

public sealed class TerminalSituationSummaryGenerator : ISituationSummaryGenerator
{
    public string Generate(StartupContext context, SupportedTerminalClient client, string prompt)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = client.Scan.BinaryPath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = context.CurrentDirectory,
        };

        foreach (string argument in client.Scan.Arguments)
        {
            startInfo.ArgumentList.Add(argument
                .Replace("{workingDirectory}", context.CurrentDirectory, StringComparison.Ordinal)
                .Replace("{prompt}", prompt, StringComparison.Ordinal));
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start {client.Name}.");

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{client.Name} compression failed: {error}");
        }

        return TerminalClientOutputExtractor.ExtractResponse(output, client);
    }
}
