using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class RemoteUrlTests
{
    [Fact]
    public void Normalize_SshAndHttpsGitHub_AreEqual()
    {
        Assert.Equal("github.com/Org/Repo", RemoteUrl.Normalize("git@github.com:Org/Repo.git"));
        Assert.Equal("github.com/Org/Repo", RemoteUrl.Normalize("https://github.com/Org/Repo"));
    }

    [Fact]
    public void Normalize_AdoHttpsWithUser()
    {
        Assert.Equal(
            "dev.azure.com/org/Project/_git/CodeArc",
            RemoteUrl.Normalize("https://org@dev.azure.com/org/Project/_git/CodeArc"));
    }

    [Fact]
    public void Normalize_AdoSshV3()
    {
        Assert.Equal("ssh.dev.azure.com/v3/org/Project/CodeArc", RemoteUrl.Normalize("git@ssh.dev.azure.com:v3/org/Project/CodeArc"));
    }

    [Fact]
    public void Normalize_SshSchemeForm()
    {
        Assert.Equal("github.com/Org/Repo", RemoteUrl.Normalize("ssh://git@github.com/Org/Repo.git"));
    }

    [Fact]
    public void Normalize_LowercasesHostOnly()
    {
        Assert.Equal("github.com/Org/Repo", RemoteUrl.Normalize("https://GitHub.COM/Org/Repo"));
    }

    [Fact]
    public void Normalize_StripsTrailingSlashAndGit()
    {
        Assert.Equal("github.com/Org/Repo", RemoteUrl.Normalize("https://github.com/Org/Repo.git/"));
        Assert.Equal("github.com/Org/Repo", RemoteUrl.Normalize("  https://github.com/Org/Repo/  "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_Empty_ReturnsNull(string? url)
    {
        Assert.Null(RemoteUrl.Normalize(url));
    }

    [Theory]
    [InlineData("git@github.com:Org/ProjectCodeArc.git", "ProjectCodeArc")]
    [InlineData("https://github.com/Org/ProjectCodeArc", "ProjectCodeArc")]
    [InlineData("https://org@dev.azure.com/org/Project/_git/CodeArc", "CodeArc")]
    [InlineData("git@ssh.dev.azure.com:v3/org/Project/CodeArc", "CodeArc")]
    [InlineData("/srv/git/local-repo.git", "local-repo")]
    public void RepositoryName_ForEachForm(string url, string expected)
    {
        Assert.Equal(expected, RemoteUrl.RepositoryName(url));
    }

    [Fact]
    public void RepositoryName_Null_ReturnsNull()
    {
        Assert.Null(RemoteUrl.RepositoryName(null));
    }
}
