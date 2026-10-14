using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class AgentHandoffPromptBuilderTests
{
    [Fact]
    public void BuildAppendsStartingPromptAsFinalSection()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();

        string prompt = AgentHandoffPromptBuilder.Build(
            Context(root, profileRoot),
            Configuration(),
            Feature(),
            "Work on existing feature.",
            "  Start by reviewing the spec.  ");

        Assert.Contains("Instruction: Work on existing feature.", prompt);
        Assert.Contains("- Slug: starting-prompt", prompt);
        Assert.EndsWith(
            $"Operate in DevCraft mode.{Environment.NewLine}{Environment.NewLine}Operator starting prompt:{Environment.NewLine}Start by reviewing the spec.",
            prompt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n  ")]
    public void BuildOmitsStartingPromptSectionWhenBlank(string startingPrompt)
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();

        string prompt = AgentHandoffPromptBuilder.Build(
            Context(root, profileRoot),
            Configuration(),
            null,
            "Work on project discovery.",
            startingPrompt);

        Assert.DoesNotContain("Operator starting prompt:", prompt);
        Assert.EndsWith("Operate in DevCraft mode.", prompt);
        Assert.Contains("No active feature was selected for this handoff.", prompt);
    }

    [Fact]
    public void BuildPreservesMultilineStartingPrompt()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string startingPrompt = "First line with \"quotes\"\n\n  - indented bullet\nLast line";

        string prompt = AgentHandoffPromptBuilder.Build(
            Context(root, profileRoot),
            Configuration(),
            Feature(),
            "Create a new feature.",
            startingPrompt);

        Assert.EndsWith($"Operator starting prompt:{Environment.NewLine}{startingPrompt}", prompt);
    }

    private static StartupContext Context(TestDirectory root, TestDirectory profileRoot)
    {
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");

        return new StartupContext(root.Path, profile, Path.Combine(profile, "soul.md"));
    }

    private static ProjectDevCraftConfiguration Configuration()
    {
        return new ProjectDevCraftConfiguration(
            Guid.NewGuid().ToString(),
            FeatureStorageMode.SystemCentral,
            "features.json",
            new ProjectProfile("Sample", "Purpose", "Description"),
            []);
    }

    private static DevCraftFeature Feature()
    {
        return new DevCraftFeature(
            "Starting prompt",
            "starting-prompt",
            "Pending.",
            Guid.NewGuid().ToString(),
            FeatureStorageMode.SystemCentral,
            null);
    }
}
