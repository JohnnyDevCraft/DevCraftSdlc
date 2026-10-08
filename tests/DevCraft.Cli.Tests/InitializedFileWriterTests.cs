using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class InitializedFileWriterTests
{
    [Fact]
    public void EnsureCreatesInitializedFile()
    {
        using TestDirectory root = new();

        InitializedFileWriter.Ensure(root.Path);

        string content = File.ReadAllText(Path.Combine(root.Path, "initialized.md"));
        Assert.Contains("soul.md", content);
        Assert.Contains("configure.json", content);
        Assert.Contains("DevCraft mode", content, StringComparison.OrdinalIgnoreCase);
    }
}
