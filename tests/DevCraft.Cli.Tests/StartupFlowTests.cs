using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class StartupFlowTests
{
    private static readonly ProjectProfile SampleProfile = new(
        "Sample",
        "Help operators validate DevCraft startup behavior.",
        "Sample is a small project used to exercise DevCraft initialization.");

    [Fact]
    public void RunCreatesMissingSoulFileAndReportsReadyForEmptyFolder()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        StartupContext context = new(root.Path, profile, soul);
        FakeConsoleInteraction console = new(
        [
            "John",
            "I build software.",
            "Jarvis",
            "Be concise.",
            "Empty Project",
            "A project created from an empty folder.",
        ]);
        FakeAiProjectScanner scanner = new(new ProjectScanResult(SampleProfile, [], new AiSdlcDetection(false, null)));
        StartupFlow flow = new(console, scanner, () =>
        [
            new TerminalAgent(TerminalAgentKind.Codex, "Codex", "codex", true),
        ]);

        flow.Run(context);

        Assert.True(File.Exists(soul));
        Assert.Contains("Default terminal AI agent: Codex", File.ReadAllText(soul));
        Assert.Contains(console.StatusMessages, message => message == "Folder ready for DevCraft");
        Assert.Contains("Scanning for profile...", console.StartupStages);
        Assert.Contains("Scanning for soul...", console.StartupStages);
        Assert.Contains("Checking for code...", console.StartupStages);
        Assert.Contains("Checking for SDLC...", console.StartupStages);
        Assert.Contains("No SDLC workflow detected.", console.StartupStages);
        Assert.True(Directory.Exists(Path.Combine(root.Path, ".devcraft")));
        Assert.True(File.Exists(Path.Combine(root.Path, ".devcraft", "AGENT.md")));
        Assert.True(File.Exists(Path.Combine(root.Path, ".devcraft", "configure.json")));
        Assert.Contains("# Empty Project", File.ReadAllText(Path.Combine(root.Path, ".devcraft", "README.md")));
        Assert.Contains("A project created from an empty folder.", File.ReadAllText(Path.Combine(root.Path, ".devcraft", "README.md")));
        Assert.False(scanner.WasCalled);
    }

    [Fact]
    public void RunCreatesMissingProfileBeforeSoulSetup()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        StartupContext context = new(root.Path, profile, soul);
        FakeConsoleInteraction console = new(
        [
            "John",
            "I build software.",
            "Jarvis",
            "Be concise.",
            "Fresh Project",
            "A fresh profile startup project.",
        ]);
        FakeAiProjectScanner scanner = new(new ProjectScanResult(SampleProfile, [], new AiSdlcDetection(false, null)));
        StartupFlow flow = new(console, scanner, () =>
        [
            new TerminalAgent(TerminalAgentKind.Codex, "Codex", "codex", true),
        ]);

        flow.Run(context);

        Assert.True(Directory.Exists(profile));
        Assert.True(Directory.Exists(Path.Combine(profile, "skills")));
        Assert.True(Directory.Exists(Path.Combine(profile, "standards")));
        Assert.True(Directory.Exists(Path.Combine(profile, "architectures")));
        Assert.True(Directory.Exists(Path.Combine(profile, "templates")));
        Assert.True(Directory.Exists(Path.Combine(profile, "project-types")));
        Assert.True(File.Exists(Path.Combine(profile, "configure.json")));
        Assert.True(File.Exists(soul));
        Assert.Contains(console.StatusMessages, message => message.StartsWith("Profile folder created:", StringComparison.Ordinal));
    }

    [Fact]
    public void RunScansFolderWithNonHiddenFile()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        File.WriteAllText(soul, "- Default terminal AI agent: Codex");
        File.WriteAllText(Path.Combine(root.Path, "Program.cs"), "Console.WriteLine();");
        StartupContext context = new(root.Path, profile, soul);
        FakeConsoleInteraction console = new([]);
        ProjectScanResult scanResult = new(
        SampleProfile,
        [
            new DetectedProject("Sample", ".NET console app"),
        ],
        new AiSdlcDetection(false, null));
        FakeAiProjectScanner scanner = new(scanResult);
        StartupFlow flow = new(console, scanner, () => []);

        flow.Run(context);

        Assert.Contains(console.StatusMessages, message => message == "Folder has code");
        Assert.Contains("Folder has code.", console.StartupStages);
        Assert.Contains(console.StatusMessages, message => message == "Scanning folder with Codex...");
        Assert.True(Directory.Exists(Path.Combine(root.Path, ".devcraft")));
        Assert.True(File.Exists(Path.Combine(root.Path, ".devcraft", "configure.json")));
        Assert.Contains("Sample is a small project", File.ReadAllText(Path.Combine(root.Path, ".devcraft", "README.md")));
        Assert.True(scanner.WasCalled);
    }

    [Fact]
    public void RunLetsOperatorOverrideAiProjectProfileBeforeInstalling()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        File.WriteAllText(soul, "- Default terminal AI agent: Codex");
        File.WriteAllText(Path.Combine(root.Path, "Program.cs"), "Console.WriteLine();");
        StartupContext context = new(root.Path, profile, soul);
        FakeConsoleInteraction console = new(
            ["Operator Project", "Operator description."],
            ["Enter a custom project name", "Enter a custom project description"]);
        ProjectScanResult scanResult = new(
            SampleProfile,
            [
                new DetectedProject("Sample", ".NET console app"),
            ],
            new AiSdlcDetection(false, null));
        FakeAiProjectScanner scanner = new(scanResult);
        StartupFlow flow = new(console, scanner, () => []);

        flow.Run(context);

        string readme = File.ReadAllText(Path.Combine(root.Path, ".devcraft", "README.md"));
        Assert.Contains("# Operator Project", readme);
        Assert.Contains("Operator description.", readme);
        Assert.DoesNotContain("# Sample", readme);
    }

    [Fact]
    public void RunDoesNotInstallDevCraftWhenAiDetectsSdlc()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        File.WriteAllText(soul, "- Default terminal AI agent: Codex");
        File.WriteAllText(Path.Combine(root.Path, "Program.cs"), "Console.WriteLine();");
        StartupContext context = new(root.Path, profile, soul);
        FakeConsoleInteraction console = new([]);
        ProjectScanResult scanResult = new(
            SampleProfile,
            [],
            new AiSdlcDetection(true, "Spec Kit"));
        FakeAiProjectScanner scanner = new(scanResult);
        StartupFlow flow = new(console, scanner, () => []);

        flow.Run(context);

        Assert.False(Directory.Exists(Path.Combine(root.Path, ".devcraft")));
        Assert.True(scanner.WasCalled);
    }

    [Fact]
    public void RunReportsAiScanFailureWithoutThrowing()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        File.WriteAllText(soul, "- Default terminal AI agent: Codex");
        File.WriteAllText(Path.Combine(root.Path, "Program.cs"), "Console.WriteLine();");
        StartupContext context = new(root.Path, profile, soul);
        FakeConsoleInteraction console = new([]);
        StartupFlow flow = new(console, new ThrowingAiProjectScanner(), () => []);

        flow.Run(context);

        Assert.Contains(console.StatusMessages, message => message.StartsWith("AI project scan failed:", StringComparison.Ordinal));
    }

    [Fact]
    public void RunReportsHiddenSdlcMarkersWithoutCallingAiScan()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".specify"));
        File.WriteAllText(soul, "- Default terminal AI agent: Codex");
        StartupContext context = new(root.Path, profile, soul);
        FakeConsoleInteraction console = new([]);
        FakeAiProjectScanner scanner = new(new ProjectScanResult(SampleProfile, [], new AiSdlcDetection(false, null)));
        StartupFlow flow = new(console, scanner, () => []);

        flow.Run(context);

        Assert.Contains(console.StatusMessages, message => message == "Folder has SDLC workflow markers");
        Assert.DoesNotContain(console.StatusMessages, message => message == "Folder ready for DevCraft");
        Assert.False(Directory.Exists(Path.Combine(root.Path, ".devcraft")));
        Assert.False(scanner.WasCalled);
    }
}
