using System.Diagnostics;
using System.Text;

namespace DevCraft.Cli;

// Runs a client's non-interactive Scan invocation in the repository and returns the agent's response text.
public static class TerminalScanRunner
{
    public static string Run(StartupContext context, SupportedTerminalClient client, string prompt, string operation)
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

        Task<string> error = process.StandardError.ReadToEndAsync();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{client.Name} {operation} failed: {error.Result}");
        }

        return TerminalClientOutputExtractor.ExtractResponse(output, client);
    }
}
