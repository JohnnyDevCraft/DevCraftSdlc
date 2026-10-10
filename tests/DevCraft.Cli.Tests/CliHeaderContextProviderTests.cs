using System.Diagnostics;
using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class CliHeaderContextProviderTests
{
    [Fact]
    public void CreateFallsBackToOriginBranchWhenUpstreamIsNotConfigured()
    {
        using TestDirectory root = new();
        string remote = Path.Combine(root.Path, "remote.git");
        string work = Path.Combine(root.Path, "work");

        RunGit(root.Path, "init", "--bare", remote);
        Directory.CreateDirectory(work);
        RunGit(work, "init", "-b", "main");
        RunGit(work, "config", "user.email", "devcraft@example.com");
        RunGit(work, "config", "user.name", "DevCraft Tests");
        File.WriteAllText(Path.Combine(work, "README.md"), "initial");
        RunGit(work, "add", "README.md");
        RunGit(work, "commit", "-m", "Initial");
        RunGit(work, "remote", "add", "origin", remote);
        RunGit(work, "push", "origin", "main");
        File.AppendAllText(Path.Combine(work, "README.md"), Environment.NewLine + "local");
        RunGit(work, "commit", "-am", "Local");

        CliHeaderContext context = CliHeaderContextProvider.Create(work);

        Assert.Equal("main", context.GitBranch);
        Assert.Equal("+1", context.GitDivergence);
    }

    private static void RunGit(string workingDirectory, params string[] arguments)
    {
        ProcessStartInfo startInfo = new("git")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)!;
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            throw new InvalidOperationException($"git {string.Join(' ', arguments)} failed: {output}{error}");
        }
    }
}
