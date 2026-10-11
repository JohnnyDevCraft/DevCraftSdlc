using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class DesktopAgentInstructionsBuilderTests
{
    private const string ConnectionString = "mongodb://user:secret@localhost:27017/DevCraft";

    [Fact]
    public void BuildNamesSoulFileBeforeAnyOtherFile()
    {
        using TestDirectory root = new();
        WriteConfiguration(root.Path, "file");

        string text = DesktopAgentInstructionsBuilder.Build(root.Path);

        string soulPath = Path.Combine(root.Path, "soul.md");
        Assert.Equal(text.IndexOf(root.Path, StringComparison.Ordinal), text.IndexOf(soulPath, StringComparison.Ordinal));
        Assert.Contains("who you should be", text);
    }

    [Fact]
    public void BuildOrdersIdentityThenSituationalAwarenessThenDevCraft()
    {
        using TestDirectory root = new();
        WriteConfiguration(root.Path, "file");

        string text = DesktopAgentInstructionsBuilder.Build(root.Path);

        int identity = text.IndexOf(Path.Combine(root.Path, "soul.md"), StringComparison.Ordinal);
        int situation = text.IndexOf("Situational Awareness:", StringComparison.Ordinal);
        int devCraft = text.IndexOf(Path.Combine(root.Path, "DevCraft.md"), StringComparison.Ordinal);
        Assert.True(identity >= 0);
        Assert.True(identity < situation);
        Assert.True(situation < devCraft);
    }

    [Fact]
    public void BuildHoldsDevCraftDormantUntilActivatePhrase()
    {
        using TestDirectory root = new();
        WriteConfiguration(root.Path, "file");

        string text = DesktopAgentInstructionsBuilder.Build(root.Path);

        Assert.Contains("Activate DevCraft", text);
        Assert.Contains(Path.Combine(root.Path, "DevCraft.md"), text);
        Assert.Contains(Path.Combine(root.Path, "initialized.md"), text);
        Assert.Contains(Path.Combine(root.Path, "configure.json"), text);
        Assert.Contains("Read these files now", text);
        Assert.Contains("do NOT apply any of their rules yet", text);
        Assert.Contains("for the rest of this thread only", text);
    }

    [Fact]
    public void BuildDoesNotActivateOnDevCraftFolderOrDefineDeactivation()
    {
        using TestDirectory root = new();
        WriteConfiguration(root.Path, "file");

        string text = DesktopAgentInstructionsBuilder.Build(root.Path);

        Assert.Contains("A repository that contains a .devcraft folder does not activate DevCraft", text);
        Assert.DoesNotContain("operate in DevCraft mode", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Deactivate", text, StringComparison.OrdinalIgnoreCase);

        string applySentence = Assert.Single(
            text.Split('\n'),
            line => line.Contains("apply the DevCraft rules", StringComparison.Ordinal));
        Assert.StartsWith("When the operator says \"Activate DevCraft\"", applySentence);
    }

    [Fact]
    public void BuildOmitsSituationalAwarenessWhenDisabled()
    {
        using TestDirectory root = new();
        WriteConfiguration(root.Path, "disabled");

        string text = DesktopAgentInstructionsBuilder.Build(root.Path);

        Assert.DoesNotContain("Situational Awareness:", text);
        Assert.DoesNotContain("\n\n\n", text);
    }

    [Fact]
    public void BuildIncludesTerminalHandoffGuidanceForFileStorage()
    {
        using TestDirectory root = new();
        WriteConfiguration(root.Path, "file");

        string text = DesktopAgentInstructionsBuilder.Build(root.Path);

        Assert.Contains(TerminalHandoffGuidance(root.Path), text);
        Assert.Contains(Path.Combine(root.Path, "situation", "people.json"), text);
    }

    [Fact]
    public void BuildIncludesTerminalHandoffGuidanceForDatabaseStorage()
    {
        using TestDirectory root = new();
        WriteConfiguration(root.Path, "database");

        string text = DesktopAgentInstructionsBuilder.Build(root.Path);

        Assert.Contains(TerminalHandoffGuidance(root.Path), text);
        Assert.Contains("Collection LogEntries", text);
        Assert.Contains("Do not paste these records back", text);
    }

    [Fact]
    public void BuildDoesNotExposeConnectionStringOrWriteGuidance()
    {
        using TestDirectory root = new();
        WriteConfiguration(root.Path, "database");

        string text = DesktopAgentInstructionsBuilder.Build(root.Path);

        Assert.DoesNotContain(ConnectionString, text);
        Assert.DoesNotContain("secret", text);
        Assert.Contains("Do not run mutating commands", text);
    }

    [Fact]
    public void BuildExplainsFeatureLookupAndProfileCatalog()
    {
        using TestDirectory root = new();
        WriteConfiguration(root.Path, "file");

        string text = DesktopAgentInstructionsBuilder.Build(root.Path);

        Assert.Contains(".devcraft/configure.json", text);
        Assert.Contains("ProjectKey", text);
        Assert.Contains("SelectedFeatureStorage", text);
        Assert.Contains(Path.Combine(root.Path, "feature-storage"), text);
        Assert.Contains(Path.Combine(root.Path, "features", "projects.json"), text);
        Assert.Contains("folder-name", text);
        Assert.Contains($"Path relative to {root.Path}", text);
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("file")]
    [InlineData("database")]
    public void BuildLeavesNoTemplateMarkersInAnyConfiguration(string mode)
    {
        using TestDirectory root = new();
        WriteConfiguration(root.Path, mode);

        string text = DesktopAgentInstructionsBuilder.Build(root.Path);

        Assert.DoesNotContain("{", text);
        Assert.DoesNotContain("}", text);
        Assert.DoesNotContain("<situational", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\n\n\n", text);
        Assert.DoesNotContain("\r", text);
        Assert.Equal(text.Trim(), text);
    }

    [Fact]
    public void BuildUsesAbsoluteProfilePaths()
    {
        using TestDirectory root = new();
        WriteConfiguration(root.Path, "file");

        string text = DesktopAgentInstructionsBuilder.Build(root.Path);

        Assert.DoesNotContain("~/.DevCraft", text);
        AssertAlwaysRooted(text, "soul.md", Path.Combine(root.Path, "soul.md"));
        AssertAlwaysRooted(text, "DevCraft.md", Path.Combine(root.Path, "DevCraft.md"));
        AssertAlwaysRooted(text, "initialized.md", Path.Combine(root.Path, "initialized.md"));
        AssertAlwaysRooted(text, "projects.json", Path.Combine(root.Path, "features", "projects.json"));
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("file")]
    [InlineData("database")]
    public void BuildStaysUnderFiveThousandCharacters(string mode)
    {
        using TestDirectory root = new();
        string padding = new('p', Math.Max(1, 120 - root.Path.Length - 1));
        string profile = Path.Combine(root.Path, padding);
        Directory.CreateDirectory(profile);
        WriteConfiguration(profile, mode);

        string text = DesktopAgentInstructionsBuilder.Build(profile);

        Assert.True(profile.Length >= 120);
        Assert.True(text.Length < 5000, $"Instructions are {text.Length} characters in {mode} mode.");
    }

    [Fact]
    public void BuildTellsAgentToReportUnreadableFiles()
    {
        using TestDirectory root = new();
        WriteConfiguration(root.Path, "file");

        string text = DesktopAgentInstructionsBuilder.Build(root.Path);

        Assert.Contains("If you cannot read a file named in these instructions, say so plainly", text);
    }

    [Fact]
    public void BuildSucceedsWhenProfileConfigurationIsMissing()
    {
        using TestDirectory root = new();

        string text = DesktopAgentInstructionsBuilder.Build(root.Path);

        Assert.Contains(Path.Combine(root.Path, "soul.md"), text);
        Assert.Contains("Activate DevCraft", text);
        Assert.DoesNotContain("Situational Awareness:", text);
    }

    private static void WriteConfiguration(string profileDirectory, string mode)
    {
        DevCraftProfileConfiguration configuration = mode switch
        {
            "file" => new DevCraftProfileConfiguration([], [], [], [], [], [], [], true),
            "database" => new DevCraftProfileConfiguration([], [], [], [], [], [], [], true, SituationScale.Sprint, SituationStorage.Database, ConnectionString),
            _ => new DevCraftProfileConfiguration([], [], [], [], [], [], []),
        };

        DevCraftProfileConfigurationWriter.Write(profileDirectory, configuration);
    }

    private static string TerminalHandoffGuidance(string profileDirectory)
    {
        return SituationPromptContextBuilder.Build(profileDirectory).ReplaceLineEndings("\n").Trim();
    }

    private static void AssertAlwaysRooted(string text, string fileName, string fullPath)
    {
        int mentions = text.Split(fileName).Length - 1;
        int rootedMentions = text.Split(fullPath).Length - 1;

        Assert.True(mentions > 0, $"{fileName} is not mentioned.");
        Assert.Equal(mentions, rootedMentions);
    }
}
