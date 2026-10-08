using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class FolderReadinessDetectorTests
{
    [Fact]
    public void DetectReportsEmptyFolderReadyForDevCraft()
    {
        using TestDirectory root = new();

        FolderReadinessResult result = FolderReadinessDetector.Detect(root.Path);

        Assert.Equal(FolderReadiness.ReadyForDevCraft, result.Readiness);
    }

    [Fact]
    public void DetectReportsHiddenOnlyFolderReadyForDevCraft()
    {
        using TestDirectory root = new();
        File.WriteAllText(Path.Combine(root.Path, ".hidden"), "ignored");

        FolderReadinessResult result = FolderReadinessDetector.Detect(root.Path);

        Assert.Equal(FolderReadiness.ReadyForDevCraft, result.Readiness);
    }

    [Fact]
    public void DetectReportsNonHiddenFileAsCode()
    {
        using TestDirectory root = new();
        File.WriteAllText(Path.Combine(root.Path, "Program.cs"), "Console.WriteLine();");

        FolderReadinessResult result = FolderReadinessDetector.Detect(root.Path);

        Assert.Equal(FolderReadiness.HasCode, result.Readiness);
    }

    [Fact]
    public void DetectReportsNonHiddenFileInsideVisibleFolderAsCode()
    {
        using TestDirectory root = new();
        string src = Path.Combine(root.Path, "src");
        Directory.CreateDirectory(src);
        File.WriteAllText(Path.Combine(src, "Program.cs"), "Console.WriteLine();");

        FolderReadinessResult result = FolderReadinessDetector.Detect(root.Path);

        Assert.Equal(FolderReadiness.HasCode, result.Readiness);
    }

    [Fact]
    public void DetectReportsVisibleFolderWithoutFilesAsCode()
    {
        using TestDirectory root = new();
        Directory.CreateDirectory(Path.Combine(root.Path, "src"));

        FolderReadinessResult result = FolderReadinessDetector.Detect(root.Path);

        Assert.Equal(FolderReadiness.HasCode, result.Readiness);
    }

    [Fact]
    public void DetectIgnoresFilesInsideHiddenFolders()
    {
        using TestDirectory root = new();
        string git = Path.Combine(root.Path, ".git");
        Directory.CreateDirectory(git);
        File.WriteAllText(Path.Combine(git, "config"), "ignored");

        FolderReadinessResult result = FolderReadinessDetector.Detect(root.Path);

        Assert.Equal(FolderReadiness.ReadyForDevCraft, result.Readiness);
    }
}
