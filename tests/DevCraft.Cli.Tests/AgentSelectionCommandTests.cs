using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class AgentSelectionCommandTests
{
    [Fact]
    public void RunPersistsSelectedInstalledAgentWithoutChangingOtherSoulContent()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        File.WriteAllText(
            soul,
            """
            # Local Soul
            - Default terminal AI agent: Codex
            - Response Style: Be concise.
            """);
        FakeConsoleInteraction console = new([], ["Claude Code (claude)"]);
        StartupContext context = new(root.Path, profile, soul);

        AgentSelectionCommand.Run(context, console, command => command == "claude");

        string content = File.ReadAllText(soul);
        Assert.Contains("- Default terminal AI agent: claude-code", content);
        Assert.Contains("- Response Style: Be concise.", content);
        Assert.Contains(console.StatusMessages, message => message == "Default AI agent set to Claude Code (claude).");
    }

    [Fact]
    public void SelectionPersistsAgentUsedByNormalStartupScan()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        File.WriteAllText(soul, "- Default terminal AI agent: Codex");
        File.WriteAllText(Path.Combine(root.Path, "Program.cs"), "Console.WriteLine();");
        StartupContext context = new(root.Path, profile, soul);
        FakeConsoleInteraction console = new(["Selected Project", "Selected description."], ["Claude Code (claude)"]);

        AgentSelectionCommand.Run(context, console, command => command == "claude");
        FakeAiProjectScanner scanner = new(new ProjectScanResult(
            new ProjectProfile("Selected", "Selected.", "Selected description."),
            [],
            new AiSdlcDetection(true, "DevCraft")));
        StartupFlow flow = new(console, scanner, () => []);

        flow.Run(context);

        Assert.Equal("claude-code", scanner.DefaultAgent);
        Assert.Equal(profile, scanner.ProfileDirectory);
        Assert.Contains(console.StatusMessages, message => message == "Scanning folder with claude-code...");
    }

    [Fact]
    public void RunDoesNotChangeSoulWhenNoAgentsAreInstalled()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        File.WriteAllText(soul, "- Default terminal AI agent: Codex");
        FakeConsoleInteraction console = new([]);
        StartupContext context = new(root.Path, profile, soul);

        AgentSelectionCommand.Run(context, console, _ => false);

        Assert.Contains("- Default terminal AI agent: Codex", File.ReadAllText(soul));
        Assert.Contains(console.StatusMessages, message => message.Contains("No installed terminal AI agents", StringComparison.Ordinal));
    }

    [Fact]
    public void RunDoesNotChangeSoulWhenSelectionIsCanceled()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        File.WriteAllText(soul, "- Default terminal AI agent: Codex");
        FakeConsoleInteraction console = new([], ["Cancel"]);
        StartupContext context = new(root.Path, profile, soul);

        AgentSelectionCommand.Run(context, console, command => command == "codex");

        Assert.Contains("- Default terminal AI agent: Codex", File.ReadAllText(soul));
        Assert.Contains(console.StatusMessages, message => message == "Default AI agent was not changed.");
    }
}
