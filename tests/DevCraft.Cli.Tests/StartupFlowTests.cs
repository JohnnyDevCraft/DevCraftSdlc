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
            "Jarvis",
        ]);
        console.EnqueueValueEdit("Empty Project");
        console.EnqueueTextEdit("I build software.");
        console.EnqueueTextEdit("Be concise.");
        console.EnqueueTextEdit("A project created from an empty folder.");
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
            "Jarvis",
        ]);
        console.EnqueueValueEdit("Fresh Project");
        console.EnqueueTextEdit("I build software.");
        console.EnqueueTextEdit("Be concise.");
        console.EnqueueTextEdit("A fresh profile startup project.");
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
        FakeConsoleInteraction console = new([]);
        console.EnqueueValueEdit("Operator Project");
        console.EnqueueTextEdit("Operator description.");
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
    public void NewProjectPrefillsFolderNameAndAcceptsItUnchanged()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        (StartupContext context, FakeConsoleInteraction console) = CreateEmptyFolderStartup(root, profileRoot);
        string folderName = Path.GetFileName(root.Path);

        new StartupFlow(console, EmptyScanner(), () => []).Run(context);

        Assert.Equal([folderName], console.ValueEditInitialValues);
        Assert.Empty(console.AskPrompts);
        Assert.Contains($"# {folderName}", ReadReadme(root));
    }

    [Fact]
    public void NewProjectUsesEditedName()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        (StartupContext context, FakeConsoleInteraction console) = CreateEmptyFolderStartup(root, profileRoot);
        console.EnqueueValueEdit("Edited Name");

        new StartupFlow(console, EmptyScanner(), () => []).Run(context);

        Assert.Contains("# Edited Name", ReadReadme(root));
    }

    [Fact]
    public void ProjectProfileCancelKeepsDefaults()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        (StartupContext context, FakeConsoleInteraction console) = CreateEmptyFolderStartup(root, profileRoot);
        console.EnqueueValueEdit("Changed", saved: false);
        console.EnqueueTextEdit("Changed description", saved: false);

        new StartupFlow(console, EmptyScanner(), () => []).Run(context);

        string readme = ReadReadme(root);
        Assert.Contains($"# {Path.GetFileName(root.Path)}", readme);
        Assert.DoesNotContain("Changed", readme);
    }

    [Fact]
    public void ProjectProfileWhitespaceNameRequiresEntry()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        (StartupContext context, FakeConsoleInteraction console) = CreateEmptyFolderStartup(root, profileRoot);
        console.EnqueueValueEdit("   ");
        console.EnqueueValueEdit("Named Project");

        new StartupFlow(console, EmptyScanner(), () => []).Run(context);

        Assert.Contains("Project name is required.", console.StatusMessages);
        Assert.Equal(2, console.ValueEditTitles.Count);
        Assert.Contains("# Named Project", ReadReadme(root));
    }

    [Fact]
    public void ProjectProfileWithoutDeterminableNameRequiresEntry()
    {
        string fileSystemRoot = Path.GetPathRoot(Path.GetTempPath())!;

        Assert.Equal(string.Empty, StartupFlow.DefaultProjectName(fileSystemRoot));
    }

    [Fact]
    public void AiScannedProjectPrefillsSuggestedNameAndDescription()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        (StartupContext context, FakeConsoleInteraction console) = CreateCodeFolderStartup(root, profileRoot);

        new StartupFlow(console, new FakeAiProjectScanner(new ProjectScanResult(SampleProfile, [], new AiSdlcDetection(false, null))), () => []).Run(context);

        Assert.Equal(["Sample"], console.ValueEditInitialValues);
        Assert.Equal([SampleProfile.Description], console.TextEditInitialTexts);
        Assert.Contains("# Sample", ReadReadme(root));
    }

    [Fact]
    public void AiScannedProjectFallsBackToFolderNameWhenSuggestionBlank()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        (StartupContext context, FakeConsoleInteraction console) = CreateCodeFolderStartup(root, profileRoot);
        ProjectProfile blankName = SampleProfile with { Name = "  " };

        new StartupFlow(console, new FakeAiProjectScanner(new ProjectScanResult(blankName, [], new AiSdlcDetection(false, null))), () => []).Run(context);

        Assert.Equal([Path.GetFileName(root.Path)], console.ValueEditInitialValues);
    }

    [Fact]
    public void AiScannedProjectDoesNotShowSuggestionMenu()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        (StartupContext context, FakeConsoleInteraction console) = CreateCodeFolderStartup(root, profileRoot);

        new StartupFlow(console, new FakeAiProjectScanner(new ProjectScanResult(SampleProfile, [], new AiSdlcDetection(false, null))), () => []).Run(context);

        Assert.DoesNotContain(console.SelectChoices.SelectMany(choices => choices), choice => choice.StartsWith("Use AI suggestion", StringComparison.Ordinal));
        Assert.Empty(console.AskPrompts);
    }

    private static FakeAiProjectScanner EmptyScanner()
    {
        return new FakeAiProjectScanner(new ProjectScanResult(SampleProfile, [], new AiSdlcDetection(false, null)));
    }

    private static (StartupContext Context, FakeConsoleInteraction Console) CreateEmptyFolderStartup(TestDirectory root, TestDirectory profileRoot)
    {
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        File.WriteAllText(soul, "- Default terminal AI agent: Codex");

        return (new StartupContext(root.Path, profile, soul), new FakeConsoleInteraction([]));
    }

    private static (StartupContext Context, FakeConsoleInteraction Console) CreateCodeFolderStartup(TestDirectory root, TestDirectory profileRoot)
    {
        (StartupContext context, FakeConsoleInteraction console) = CreateEmptyFolderStartup(root, profileRoot);
        File.WriteAllText(Path.Combine(root.Path, "Program.cs"), "Console.WriteLine();");

        return (context, console);
    }

    private static string ReadReadme(TestDirectory root)
    {
        return File.ReadAllText(Path.Combine(root.Path, ".devcraft", "README.md"));
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

    [Fact]
    public void RunInstallsDevCraftWhenForcedDespiteOtherSdlcMarkers()
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
        console.EnqueueValueEdit("Forced Project");
        FakeAiProjectScanner scanner = new(new ProjectScanResult(SampleProfile, [], new AiSdlcDetection(false, null)));
        StartupFlow flow = new(console, scanner, () => []);

        flow.Run(context, forceInstall: true);

        Assert.True(Directory.Exists(Path.Combine(root.Path, ".devcraft")));
        Assert.True(File.Exists(Path.Combine(root.Path, ".devcraft", "configure.json")));
        Assert.Equal(DevCraftCurrentVersionProvider.Get(), File.ReadAllText(Path.Combine(root.Path, ".devcraft", "version")).Trim());
        Assert.False(File.Exists(Path.Combine(root.Path, "AGENT.md")));
        Assert.True(Directory.Exists(Path.Combine(root.Path, ".specify")));
        Assert.False(scanner.WasCalled);
    }

    [Fact]
    public void RunInstallsDevCraftWhenForcedInCodeFolderWithoutScanning()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        File.WriteAllText(Path.Combine(root.Path, "Program.cs"), "Console.WriteLine(\"hello\");");
        File.WriteAllText(soul, "- Default terminal AI agent: Codex");
        StartupContext context = new(root.Path, profile, soul);
        FakeConsoleInteraction console = new([]);
        console.EnqueueValueEdit("Forced Code Project");
        FakeAiProjectScanner scanner = new(new ProjectScanResult(SampleProfile, [], new AiSdlcDetection(true, "Other AI workflow")));
        StartupFlow flow = new(console, scanner, () => []);

        flow.Run(context, forceInstall: true);

        Assert.True(Directory.Exists(Path.Combine(root.Path, ".devcraft")));
        Assert.True(File.Exists(Path.Combine(root.Path, ".devcraft", "configure.json")));
        Assert.Equal(DevCraftCurrentVersionProvider.Get(), File.ReadAllText(Path.Combine(root.Path, ".devcraft", "version")).Trim());
        Assert.False(File.Exists(Path.Combine(root.Path, "AGENT.md")));
        Assert.False(scanner.WasCalled);
    }

    [Fact]
    public void RunForceInCurrentDevCraftFolderDoesNotLaunchUpgradeAgent()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        File.WriteAllText(soul, "- Default terminal AI agent: Codex");
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        DevCraftRepositoryVersionStore.Write(root.Path, DevCraftCurrentVersionProvider.Get());
        StartupContext context = new(root.Path, profile, soul);
        FakeConsoleInteraction console = new([]);
        FakeDevCraftRepositoryUpgradeLauncher upgradeLauncher = new();
        StartupFlow flow = new(
            console,
            new FakeAiProjectScanner(new ProjectScanResult(SampleProfile, [], new AiSdlcDetection(false, null))),
            () => [],
            upgradeLauncher);

        bool shouldContinue = flow.Run(context, forceInstall: true);

        Assert.True(shouldContinue);
        Assert.Empty(upgradeLauncher.Requests);
        Assert.Contains(console.StatusMessages, message => message.Contains("current", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RunForceInMissingVersionDevCraftFolderLaunchesUpgradeAgentAndWritesVersion()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        File.WriteAllText(soul, "- Default terminal AI agent: Codex");
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, soul);
        FakeConsoleInteraction console = new([]);
        FakeDevCraftRepositoryUpgradeLauncher upgradeLauncher = new();
        StartupFlow flow = new(
            console,
            new FakeAiProjectScanner(new ProjectScanResult(SampleProfile, [], new AiSdlcDetection(false, null))),
            () => [],
            upgradeLauncher);

        bool shouldContinue = flow.Run(context, forceInstall: true);

        Assert.True(shouldContinue);
        DevCraftRepositoryUpgradeRequest request = Assert.Single(upgradeLauncher.Requests);
        Assert.Null(request.RepositoryVersion);
        Assert.Equal(FeatureStorageMode.SystemCentral, request.EffectiveFeatureStorage);
        Assert.Equal(DevCraftCurrentVersionProvider.Get(), File.ReadAllText(Path.Combine(root.Path, ".devcraft", "version")).Trim());
    }

    [Fact]
    public void RunForce_AgentDropsKeyAndProfile_DevCraftRestoresThem()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        File.WriteAllText(soul, "- Default terminal AI agent: Codex");
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        string key = ProjectDevCraftConfigurationReader.Read(root.Path)!.ProjectKey;
        FakeDevCraftRepositoryUpgradeLauncher upgrade = new()
        {
            OnUpgrade = request => File.WriteAllText(
                ProjectDevCraftConfigurationStore.PathFor(request.Context.CurrentDirectory),
                """{ "SelectedFeatureStorage": "system-central", "FeaturesIndexPath": "features.json" }"""),
        };

        new StartupFlow(new FakeConsoleInteraction([]), new FakeAiProjectScanner(new ProjectScanResult(SampleProfile, [], new AiSdlcDetection(false, null))), () => [], upgrade)
            .Run(new StartupContext(root.Path, profile, soul), forceInstall: true);

        ProjectDevCraftConfiguration repaired = ProjectDevCraftConfigurationReader.Read(root.Path)!;
        Assert.Equal(key, repaired.ProjectKey);
        Assert.Equal(SampleProfile, repaired.ProjectProfile);
        Assert.Equal("system-central", repaired.SelectedFeatureStorage);
    }

    [Fact]
    public void RunForce_KeylessConfig_GetsKeyBeforeUpgrade()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        File.WriteAllText(soul, "- Default terminal AI agent: Codex");
        File.WriteAllText(ProjectDevCraftConfigurationStore.PathFor(root.Path), """{ "SelectedFeatureStorage": "system-central" }""");
        FakeDevCraftRepositoryUpgradeLauncher upgrade = new();

        new StartupFlow(new FakeConsoleInteraction([]), new FakeAiProjectScanner(new ProjectScanResult(SampleProfile, [], new AiSdlcDetection(false, null))), () => [], upgrade)
            .Run(new StartupContext(root.Path, profile, soul), forceInstall: true);

        DevCraftRepositoryUpgradeRequest request = Assert.Single(upgrade.Requests);
        Assert.True(Guid.TryParse(request.ProjectConfiguration.ProjectKey, out _));
        ProjectDevCraftConfiguration saved = ProjectDevCraftConfigurationReader.Read(root.Path)!;
        Assert.Equal(request.ProjectConfiguration.ProjectKey, saved.ProjectKey);
        Assert.Equal(Path.GetFileName(root.Path), saved.ProjectProfile.Name);
    }

    [Fact]
    public void RunForceInDevCraftFolderWithMissingConfigurationLaunchesUpgradeAgent()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        File.WriteAllText(soul, "- Default terminal AI agent: Codex");
        StartupContext context = new(root.Path, profile, soul);
        FakeConsoleInteraction console = new([]);
        FakeDevCraftRepositoryUpgradeLauncher upgradeLauncher = new();
        StartupFlow flow = new(
            console,
            new FakeAiProjectScanner(new ProjectScanResult(SampleProfile, [], new AiSdlcDetection(false, null))),
            () => [],
            upgradeLauncher);

        bool shouldContinue = flow.Run(context, forceInstall: true);

        Assert.True(shouldContinue);
        DevCraftRepositoryUpgradeRequest request = Assert.Single(upgradeLauncher.Requests);
        Assert.Null(request.RepositoryVersion);
        Assert.Equal(Path.GetFileName(root.Path), request.ProjectConfiguration.ProjectProfile.Name);
        Assert.Equal(FeatureStorageMode.SystemCentral, request.EffectiveFeatureStorage);
        Assert.Equal(DevCraftCurrentVersionProvider.Get(), File.ReadAllText(Path.Combine(root.Path, ".devcraft", "version")).Trim());
        Assert.DoesNotContain(console.StatusMessages, message => message.Contains("configuration is missing", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RunForceInOlderVersionDevCraftFolderLaunchesUpgradeAgent()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        File.WriteAllText(soul, "- Default terminal AI agent: Codex");
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        DevCraftRepositoryVersionStore.Write(root.Path, "1.0.0-beta.10");
        StartupContext context = new(root.Path, profile, soul);
        FakeConsoleInteraction console = new([]);
        FakeDevCraftRepositoryUpgradeLauncher upgradeLauncher = new();
        StartupFlow flow = new(
            console,
            new FakeAiProjectScanner(new ProjectScanResult(SampleProfile, [], new AiSdlcDetection(false, null))),
            () => [],
            upgradeLauncher);

        bool shouldContinue = flow.Run(context, forceInstall: true);

        Assert.True(shouldContinue);
        DevCraftRepositoryUpgradeRequest request = Assert.Single(upgradeLauncher.Requests);
        Assert.Equal("1.0.0-beta.10", request.RepositoryVersion);
        Assert.Equal(DevCraftCurrentVersionProvider.Get(), request.CurrentVersion);
    }

    [Fact]
    public void RunForceFailedUpgradeLeavesVersionUnchangedAndStopsStartup()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        File.WriteAllText(soul, "- Default terminal AI agent: Codex");
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        DevCraftRepositoryVersionStore.Write(root.Path, "1.0.0-beta.10");
        StartupContext context = new(root.Path, profile, soul);
        FakeConsoleInteraction console = new([]);
        FakeDevCraftRepositoryUpgradeLauncher upgradeLauncher = new()
        {
            Result = DevCraftRepositoryUpgradeResult.Failed("No agent."),
        };
        StartupFlow flow = new(
            console,
            new FakeAiProjectScanner(new ProjectScanResult(SampleProfile, [], new AiSdlcDetection(false, null))),
            () => [],
            upgradeLauncher);

        bool shouldContinue = flow.Run(context, forceInstall: true);

        Assert.False(shouldContinue);
        Assert.Single(upgradeLauncher.Requests);
        Assert.Equal("1.0.0-beta.10", File.ReadAllText(Path.Combine(root.Path, ".devcraft", "version")).Trim());
        Assert.Contains(console.StatusMessages, message => message.Contains("upgrade failed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RunPrefersExistingDevCraftWhenOtherSdlcMarkersAlsoExist()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        Directory.CreateDirectory(Path.Combine(root.Path, ".specify"));
        File.WriteAllText(soul, "- Default terminal AI agent: Codex");
        StartupContext context = new(root.Path, profile, soul);
        FakeConsoleInteraction console = new([]);
        FakeAiProjectScanner scanner = new(new ProjectScanResult(SampleProfile, [], new AiSdlcDetection(false, null)));
        StartupFlow flow = new(console, scanner, () => []);

        flow.Run(context);

        Assert.Contains(console.StatusMessages, message => message == "Folder already uses DevCraft");
        Assert.DoesNotContain(console.StatusMessages, message => message == "Folder has SDLC workflow markers");
        Assert.False(scanner.WasCalled);
    }
}
