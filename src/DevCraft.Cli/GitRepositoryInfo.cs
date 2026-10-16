namespace DevCraft.Cli;

// What git says about a working copy. Branch is null on a detached HEAD; an unborn branch is still named.
public sealed record GitRepositoryInfo(bool IsRepository, string? Branch, string? RemoteUrl)
{
    public static readonly GitRepositoryInfo None = new(false, null, null);

    public static GitRepositoryInfo Read(IGitRunner git, string directory)
    {
        try
        {
            if (git.Run(directory, ["rev-parse", "--is-inside-work-tree"]).ExitCode != 0)
            {
                return None;
            }

            return new GitRepositoryInfo(
                true,
                Output(git.Run(directory, ["symbolic-ref", "--short", "-q", "HEAD"])),
                Output(git.Run(directory, ["remote", "get-url", "origin"])));
        }
        catch (InvalidOperationException)
        {
            return None;
        }
    }

    private static string? Output(TerminalProcessResult result)
    {
        string value = result.StandardOutput.Trim();

        return result.ExitCode != 0 || value.Length == 0 ? null : value;
    }
}
