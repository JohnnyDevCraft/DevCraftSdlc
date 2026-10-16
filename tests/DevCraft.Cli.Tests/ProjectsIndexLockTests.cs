using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class ProjectsIndexLockTests
{
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(100);

    [Fact]
    public void SecondAcquire_WhileHeld_ThrowsBusyAfterTimeout()
    {
        using TestDirectory profile = new();
        using IDisposable held = ProjectsIndexLock.Acquire(profile.Path, ShortTimeout);

        ProjectsIndexBusyException busy = Assert.Throws<ProjectsIndexBusyException>(
            () => ProjectsIndexLock.Acquire(profile.Path, ShortTimeout));

        Assert.Equal("projects.json is busy in another DevCraft session. Try again.", busy.Message);
    }

    [Fact]
    public void Acquire_AfterDispose_Succeeds()
    {
        using TestDirectory profile = new();
        ProjectsIndexLock.Acquire(profile.Path, ShortTimeout).Dispose();

        using IDisposable second = ProjectsIndexLock.Acquire(profile.Path, ShortTimeout);

        Assert.NotNull(second);
    }

    [Fact]
    public void LeftoverLockFile_WithoutHandle_DoesNotBlock()
    {
        using TestDirectory profile = new();
        string lockPath = Path.Combine(profile.Path, "features", "projects.json.lock");
        Directory.CreateDirectory(Path.GetDirectoryName(lockPath)!);
        File.WriteAllText(lockPath, string.Empty);

        using IDisposable acquired = ProjectsIndexLock.Acquire(profile.Path, ShortTimeout);

        Assert.True(File.Exists(lockPath));
    }
}
