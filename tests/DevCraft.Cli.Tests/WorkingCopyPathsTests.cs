using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class WorkingCopyPathsTests
{
    [Fact]
    public void Normalize_TrimsTrailingSeparator()
    {
        string path = Path.Combine(Path.GetTempPath(), "Active Work 1", "ProjectCodeArc");

        Assert.Equal(path, WorkingCopyPaths.Normalize(path + Path.DirectorySeparatorChar));
    }

    [Fact]
    public void Same_IgnoreCaseTrue_MatchesDifferentCase()
    {
        Assert.True(WorkingCopyPaths.Same("/repos/Active Work 1/ProjectCodeArc", "/REPOS/active work 1/projectcodearc/", ignoreCase: true));
    }

    [Fact]
    public void Same_IgnoreCaseFalse_DistinguishesCase()
    {
        Assert.False(WorkingCopyPaths.Same("/repos/Active Work 1/ProjectCodeArc", "/repos/active work 1/ProjectCodeArc", ignoreCase: false));
        Assert.True(WorkingCopyPaths.Same("/repos/Active Work 1/ProjectCodeArc", "/repos/Active Work 1/ProjectCodeArc/", ignoreCase: false));
    }
}
