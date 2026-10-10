using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class DisplayPathFormatterTests
{
    [Fact]
    public void ToDisplayPathShortensHomePath()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string path = Path.Combine(home, "Source", "repos", "DevCraft");

        string displayPath = DisplayPathFormatter.ToDisplayPath(path);

        Assert.Equal($"~{Path.DirectorySeparatorChar}Source{Path.DirectorySeparatorChar}repos{Path.DirectorySeparatorChar}DevCraft", displayPath);
    }
}
