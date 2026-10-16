using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class GitRepositoryInfoTests
{
    [Fact]
    public void OnBranch_ReturnsBranchAndRemote()
    {
        FakeGitRunner git = new FakeGitRunner()
            .On("symbolic-ref --short -q HEAD", output: "john/story/1234-linking\n")
            .On("remote get-url origin", output: "git@github.com:Org/Repo.git\n");

        GitRepositoryInfo info = GitRepositoryInfo.Read(git, "/repo");

        Assert.True(info.IsRepository);
        Assert.Equal("john/story/1234-linking", info.Branch);
        Assert.Equal("git@github.com:Org/Repo.git", info.RemoteUrl);
    }

    [Fact]
    public void DetachedHead_BranchNull()
    {
        FakeGitRunner git = new FakeGitRunner().On("symbolic-ref --short -q HEAD", exitCode: 1);

        Assert.Null(GitRepositoryInfo.Read(git, "/repo").Branch);
    }

    [Fact]
    public void NoOrigin_RemoteNull()
    {
        FakeGitRunner git = new FakeGitRunner().On("remote get-url origin", exitCode: 2, error: "error: No such remote 'origin'");

        Assert.Null(GitRepositoryInfo.Read(git, "/repo").RemoteUrl);
    }

    [Fact]
    public void NotARepository_IsRepositoryFalse()
    {
        FakeGitRunner git = new FakeGitRunner().On("rev-parse --is-inside-work-tree", exitCode: 128);

        GitRepositoryInfo info = GitRepositoryInfo.Read(git, "/folder");

        Assert.Equal(GitRepositoryInfo.None, info);
        Assert.DoesNotContain("symbolic-ref --short -q HEAD", git.Calls);
    }

    [Fact]
    public void GitMissing_TreatedAsNotARepository()
    {
        GitRepositoryInfo info = GitRepositoryInfo.Read(new ThrowingGitRunner(), "/folder");

        Assert.False(info.IsRepository);
    }

    private sealed class ThrowingGitRunner : IGitRunner
    {
        public TerminalProcessResult Run(string workingDirectory, IReadOnlyList<string> arguments)
        {
            throw new InvalidOperationException("Git is not installed or is not on your PATH.");
        }
    }
}
