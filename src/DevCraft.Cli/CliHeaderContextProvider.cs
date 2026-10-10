using System.Diagnostics;

namespace DevCraft.Cli;

internal static class CliHeaderContextProvider
{
    private const int GitTimeoutMilliseconds = 750;

    public static CliHeaderContext Create(string directory)
    {
        string displayDirectory = DisplayPathFormatter.ToDisplayPath(directory);
        string branch = GetGitBranch(directory) ?? "no git";
        string? divergence = branch == "no git" ? null : GetGitDivergence(directory, branch);

        return new CliHeaderContext(displayDirectory, branch, divergence);
    }

    private static string? GetGitBranch(string directory)
    {
        string? branch = RunGit(directory, "rev-parse", "--abbrev-ref", "HEAD");

        if (string.IsNullOrWhiteSpace(branch))
        {
            return null;
        }

        if (!string.Equals(branch, "HEAD", StringComparison.Ordinal))
        {
            return branch;
        }

        return RunGit(directory, "rev-parse", "--short", "HEAD");
    }

    private static string? GetGitDivergence(string directory, string branch)
    {
        string? comparisonRef = RunGit(directory, "rev-parse", "--abbrev-ref", "--symbolic-full-name", "@{u}");

        if (string.IsNullOrWhiteSpace(comparisonRef))
        {
            string originBranch = $"origin/{branch}";
            string? remoteBranch = RunGit(directory, "rev-parse", "--verify", "--quiet", originBranch);
            comparisonRef = string.IsNullOrWhiteSpace(remoteBranch) ? null : originBranch;
        }

        if (string.IsNullOrWhiteSpace(comparisonRef))
        {
            return null;
        }

        string? counts = RunGit(directory, "rev-list", "--left-right", "--count", $"{comparisonRef}...HEAD");

        if (string.IsNullOrWhiteSpace(counts))
        {
            return null;
        }

        string[] parts = counts.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2
            || !int.TryParse(parts[0], out int behind)
            || !int.TryParse(parts[1], out int ahead))
        {
            return null;
        }

        return (ahead, behind) switch
        {
            (> 0, > 0) => $"+{ahead} -{behind}",
            (> 0, _) => $"+{ahead}",
            (_, > 0) => $"-{behind}",
            _ => "+0",
        };
    }

    private static string? RunGit(string directory, params string[] arguments)
    {
        try
        {
            ProcessStartInfo startInfo = new("git")
            {
                WorkingDirectory = directory,
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

            if (!process.WaitForExit(GitTimeoutMilliseconds))
            {
                process.Kill(entireProcessTree: true);

                return null;
            }

            if (process.ExitCode != 0)
            {
                return null;
            }

            return process.StandardOutput.ReadToEnd().Trim();
        }
        catch
        {
            return null;
        }
    }
}
