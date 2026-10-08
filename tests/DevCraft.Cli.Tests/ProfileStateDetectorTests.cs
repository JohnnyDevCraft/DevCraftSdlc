using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class ProfileStateDetectorTests
{
    [Fact]
    public void DetectReportsPresentProfileAndSoul()
    {
        using TestDirectory root = new();
        string profile = Path.Combine(root.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        File.WriteAllText(soul, "# SOUL");
        StartupContext context = new(root.Path, profile, soul);

        ProfileState state = ProfileStateDetector.Detect(context);

        Assert.True(state.ProfileDirectoryExists);
        Assert.True(state.SoulFileExists);
    }

    [Fact]
    public void DetectReportsMissingProfileAndSoul()
    {
        using TestDirectory root = new();
        string profile = Path.Combine(root.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        StartupContext context = new(root.Path, profile, soul);

        ProfileState state = ProfileStateDetector.Detect(context);

        Assert.False(state.ProfileDirectoryExists);
        Assert.False(state.SoulFileExists);
    }
}

