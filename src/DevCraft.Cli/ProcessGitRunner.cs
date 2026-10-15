using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace DevCraft.Cli;

// Runs git without a shell. Standard input stays attached to the terminal so credential helpers can prompt.
public sealed class ProcessGitRunner : IGitRunner
{
    public TerminalProcessResult Run(string workingDirectory, IReadOnlyList<string> arguments)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "git",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = workingDirectory,
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        Process? started;

        try
        {
            started = Process.Start(startInfo);
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException("Git is not installed or is not on your PATH.", exception);
        }

        using Process process = started ?? throw new InvalidOperationException("Could not start git.");
        Task<string> error = process.StandardError.ReadToEndAsync();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return new TerminalProcessResult(process.ExitCode, output, error.Result);
    }
}
