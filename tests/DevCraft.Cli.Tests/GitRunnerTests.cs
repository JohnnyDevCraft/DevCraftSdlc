using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class GitRunnerTests
{
    [Fact]
    public void ProcessGitRunnerRunsGitInTheWorkingDirectory()
    {
        using TestDirectory root = new();
        ProcessGitRunner git = new();

        TerminalProcessResult init = git.Run(root.Path, ["init", "--quiet"]);
        TerminalProcessResult inside = git.Run(root.Path, ["rev-parse", "--is-inside-work-tree"]);

        Assert.Equal(0, init.ExitCode);
        Assert.Equal(0, inside.ExitCode);
        Assert.Equal("true", inside.StandardOutput.Trim());
    }

    [Fact]
    public void ProcessGitRunnerReportsFailuresWithExitCodeAndError()
    {
        using TestDirectory root = new();

        TerminalProcessResult result = new ProcessGitRunner().Run(root.Path, ["rev-parse", "--is-inside-work-tree"]);

        Assert.NotEqual(0, result.ExitCode);
        Assert.NotEmpty(result.StandardError);
    }
    [Fact]
    public void GitFeatureActionsWorkAgainstRealRepositoryWithOrigin()
    {
        using TestDirectory root = new();
        string origin = Path.Combine(root.Path, "origin.git");
        string work = Path.Combine(root.Path, "work");
        ProcessGitRunner git = new();
        Assert.Equal(0, git.Run(root.Path, ["-c", "init.defaultBranch=main", "init", "--bare", "--quiet", origin]).ExitCode);
        Assert.Equal(0, git.Run(root.Path, ["-c", "init.defaultBranch=main", "init", "--quiet", work]).ExitCode);
        git.Run(work, ["config", "user.email", "test@example.com"]);
        git.Run(work, ["config", "user.name", "Test"]);
        git.Run(work, ["config", "commit.gpgsign", "false"]);
        git.Run(work, ["remote", "add", "origin", origin]);
        File.WriteAllText(Path.Combine(work, "a.txt"), "one\n");
        StartupContext context = new(work, root.Path, Path.Combine(root.Path, "soul.md"));
        FakeRepositoryDocumentAgent agent = new() { Response = """{ "success": true, "summary": "", "message": "Add a" }""" };
        FakeConsoleInteraction console = new([], ["Enter a custom branch name", "main", "topic/b"]);
        console.EnqueueValueEdit("topic/b");
        GitFeatureActions actions = new(context, console, git, agent);
        DevCraftFeature feature = new("B", "b", "Pending.", "folder", "system-central", null);

        string committed = actions.CommitCode();
        string? created = actions.CreateBranch(feature);
        File.WriteAllText(Path.Combine(work, "b.txt"), "two\n");
        string committedOnTopic = actions.CommitCode();
        string? switched = actions.SwitchAndPull();
        string? merged = actions.MergeFrom();

        Assert.Equal("Committed to main: Add a. Pushed main to origin and set it as the upstream.", committed);
        Assert.Equal("Created branch topic/b from main and switched to it.", created);
        Assert.Equal("Committed to topic/b: Add a. Pushed topic/b to origin and set it as the upstream.", committedOnTopic);
        Assert.Equal("Switched to main and pulled the latest commits from origin.", switched);
        Assert.Equal("Merged topic/b into main.", merged);
        Assert.True(File.Exists(Path.Combine(work, "b.txt")));
        Assert.Equal("main", git.Run(work, ["rev-parse", "--abbrev-ref", "HEAD"]).StandardOutput.Trim());
    }

    [Fact]
    public void GitRepositoryInfo_AgainstRealRepo_UnbornBranchReportsName()
    {
        using TestDirectory root = new();
        ProcessGitRunner git = new();
        git.Run(root.Path, ["-c", "init.defaultBranch=main", "init", "--quiet"]);
        git.Run(root.Path, ["remote", "add", "origin", "git@github.com:Org/ProjectCodeArc.git"]);

        GitRepositoryInfo info = GitRepositoryInfo.Read(git, root.Path);

        Assert.True(info.IsRepository);
        Assert.Equal("main", info.Branch);
        Assert.Equal("git@github.com:Org/ProjectCodeArc.git", info.RemoteUrl);
    }

    [Fact]
    public void GitRepositoryInfo_AgainstPlainFolder_IsNotARepository()
    {
        using TestDirectory root = new();

        Assert.False(GitRepositoryInfo.Read(new ProcessGitRunner(), root.Path).IsRepository);
    }
}
