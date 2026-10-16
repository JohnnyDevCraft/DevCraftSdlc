using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class DevCraftMenuCommandTests : IDisposable
{
    private static readonly ProjectProfile SampleProfile = new(
        "Sample Project",
        "Validate menu behavior.",
        "A sample project for menu command tests.");

    public DevCraftMenuCommandTests()
    {
        DevCraftMenuCommand.CommandExists = command => command == "codex";
    }

    public void Dispose()
    {
        DevCraftMenuCommand.CommandExists = CommandLocator.Exists;
    }

    [Fact]
    public void RunLaunchesProjectDiscovery()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Project Management", "Project discovery", "OpenAI Codex (codex)", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        string instruction = Assert.Single(launcher.Instructions);
        Assert.Contains("project discovery", instruction, StringComparison.OrdinalIgnoreCase);
        Assert.True(console.MenuShellCount > 0);
        Assert.Empty(featureLauncher.Launches);
    }

    [Fact]
    public void RunProjectDiscoveryAsksStartingPromptBeforeClientAndPassesIt()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        FakeConsoleInteraction console = new([], ["Project Management", "Project discovery", "OpenAI Codex (codex)", "Exit"]);
        console.EnqueueTextEdit("Let's define the MVP.");
        FakeDevCraftAiSessionLauncher launcher = new();

        DevCraftMenuCommand.Run(context, console, launcher, new FakeFeatureAiSessionLauncher());

        int promptIndex = console.Events.IndexOf("text:Starting Prompt");
        int clientIndex = console.Events.IndexOf("select:Which AI client should I use?");
        Assert.True(promptIndex >= 0);
        Assert.True(clientIndex > promptIndex);
        Assert.Equal("Let's define the MVP.", Assert.Single(launcher.StartingPrompts));
    }

    [Fact]
    public void RunProjectDiscoveryCancelledStartingPromptReturnsToMenuWithoutLaunching()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        FakeConsoleInteraction console = new([], ["Project Management", "Project discovery", "Exit"]);
        console.EnqueueTextEdit("Discarded", saved: false);
        FakeDevCraftAiSessionLauncher launcher = new();

        DevCraftMenuCommand.Run(context, console, launcher, new FakeFeatureAiSessionLauncher());

        Assert.Empty(launcher.Launches);
        Assert.DoesNotContain("Which AI client should I use?", console.SelectTitles);
        Assert.Contains(console.SelectTitles, title => title.StartsWith("Agent handoff cancelled.", StringComparison.Ordinal));
    }

    [Fact]
    public void RunNewFeatureCancelledStartingPromptCreatesNoFeature()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        FakeConsoleInteraction console = new([], ["Features", "New feature", "Exit"]);
        console.EnqueueFieldEdit(NewFeatureForm("Cancelled Feature"));
        console.EnqueueTextEdit("Discarded", saved: false);
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), featureLauncher);

        Assert.Empty(featureLauncher.Launches);
        Assert.Empty(ProjectDevCraftConfigurationReader.Read(root.Path)!.Features);
        Assert.DoesNotContain("Which AI client should I use?", console.SelectTitles);
    }

    [Fact]
    public void RunNewFeaturePassesStartingPromptToFeatureLauncher()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        FakeConsoleInteraction console = new([], ["Features", "New feature", "OpenAI Codex (codex)", "Exit"]);
        console.EnqueueFieldEdit(NewFeatureForm("Prompted Feature"));
        console.EnqueueTextEdit("Here is what this feature is about.");
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), featureLauncher);

        Assert.Single(featureLauncher.Launches);
        Assert.Equal("Here is what this feature is about.", Assert.Single(featureLauncher.StartingPrompts));
        Assert.True(console.Events.IndexOf("text:Starting Prompt") < console.Events.IndexOf("select:Which AI client should I use?"));
    }

    [Fact]
    public void RunImportSpecKitConstitutionWithoutConstitutionDoesNotCallAgent()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        FakeConsoleInteraction console = new([], ["Project Management", "Import SpecKit Constitution", "Exit"]);
        FakeRepositoryDocumentAgent agent = new();

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher(), documentAgent: agent);

        Assert.Empty(agent.Prompts);
        Assert.StartsWith("No SpecKit constitution found at .specify/memory/constitution.md.", console.SelectTitles.Last(), StringComparison.Ordinal);
    }

    [Fact]
    public void RunImportSpecKitConstitutionRunsDefaultAgentHeadlessAndWritesChangedFiles()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        WriteConstitution(root.Path);
        File.WriteAllText(context.SoulFilePath, "- Default terminal AI agent: Claude AI\n");
        FakeConsoleInteraction console = new([], ["Project Management", "Import SpecKit Constitution", "Exit"]);
        FakeRepositoryDocumentAgent agent = new()
        {
            Response = """{"success":true,"summary":"Added principles.","files":[{"name":"GOV.md","content":"# GOV.md\n\nPrinciple I\n"},{"name":"src/App.cs","content":"x"}]}""",
        };

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher(), documentAgent: agent);

        Assert.Equal("claude-code", Assert.Single(agent.Clients).Slug);
        Assert.Contains(Path.Combine(root.Path, ".specify", "memory", "constitution.md"), Assert.Single(agent.Prompts));
        Assert.Contains("Reading the SpecKit constitution with Claude Code...", console.StatusMessages);
        Assert.DoesNotContain("Which AI client should I use?", console.SelectTitles);
        Assert.DoesNotContain("Starting Prompt", console.TextEditTitles);
        Assert.Equal("# GOV.md\n\nPrinciple I\n", File.ReadAllText(Path.Combine(root.Path, ".devcraft", "GOV.md")));
        Assert.False(File.Exists(Path.Combine(root.Path, ".devcraft", "src", "App.cs")));
        string notice = console.SelectTitles.Last();
        Assert.Contains("Updated GOV.md.", notice);
        Assert.Contains("src/App.cs", notice);
        Assert.Contains("Added principles.", notice);
    }

    [Fact]
    public void RunImportSpecKitConstitutionTwiceReportsNoChangesWhenFilesMatch()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        WriteConstitution(root.Path);
        FakeConsoleInteraction console = new([], ["Project Management", "Import SpecKit Constitution", "Project Management", "Import SpecKit Constitution", "Yes", "Exit"]);
        FakeRepositoryDocumentAgent agent = new()
        {
            Response = """{"success":true,"summary":"","files":[{"name":"GOV.md","content":"# GOV.md\n"}]}""",
        };

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher(), documentAgent: agent);

        Assert.Equal(2, agent.Prompts.Count);
        Assert.Contains("No DevCraft files changed", console.SelectTitles.Last());
    }

    [Fact]
    public void RunImportSpecKitConstitutionReportsAgentFailureWithoutWriting()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        WriteConstitution(root.Path);
        FakeConsoleInteraction console = new([], ["Project Management", "Import SpecKit Constitution", "Exit"]);
        FakeRepositoryDocumentAgent agent = new() { Response = "I could not do it." };

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher(), documentAgent: agent);

        Assert.StartsWith("SpecKit constitution import failed:", console.SelectTitles.Last(), StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root.Path, ".devcraft", "GOV.md")));
    }

    [Fact]
    public void RunInferProjectDetailsUsesSpecKitSourcesWhenPresentAndWritesAllowedFiles()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        WriteConstitution(root.Path);
        Directory.CreateDirectory(Path.Combine(root.Path, "specs", "001-login"));
        FakeConsoleInteraction console = new([], ["Project Management", "Infer Project Details", "Exit"]);
        FakeRepositoryDocumentAgent agent = new()
        {
            Response = """{"success":true,"summary":"Inferred.","files":[{"name":"ARCH.md","content":"# ARCH.md\n"},{"name":"MODEL.md","content":"# MODEL.md\n"},{"name":"AGENT.md","content":"x"}]}""",
        };

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher(), documentAgent: agent);

        string prompt = Assert.Single(agent.Prompts);
        Assert.Contains(Path.Combine(root.Path, "specs"), prompt);
        Assert.Contains(Path.Combine(root.Path, ".specify", "memory", "constitution.md"), prompt);
        Assert.Contains("Inferring project details with OpenAI Codex...", console.StatusMessages);
        Assert.True(File.Exists(Path.Combine(root.Path, ".devcraft", "ARCH.md")));
        Assert.True(File.Exists(Path.Combine(root.Path, ".devcraft", "MODEL.md")));
        Assert.False(File.Exists(Path.Combine(root.Path, ".devcraft", "AGENT.md")));
        Assert.Contains("Updated ARCH.md, MODEL.md.", console.SelectTitles.Last());
    }

    [Fact]
    public void RunImportSpecKitConstitutionRecordsConstitutionMd5InDevCraftReadme()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        WriteConstitution(root.Path);
        string readmePath = Path.Combine(root.Path, ".devcraft", "README.md");
        File.WriteAllText(readmePath, "# README.md\n\n## Purpose\n\nKeep me.\n");
        FakeConsoleInteraction console = new([], ["Project Management", "Import SpecKit Constitution", "Exit"]);

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher(), documentAgent: new FakeRepositoryDocumentAgent());

        string expectedHash = Convert.ToHexStringLower(System.Security.Cryptography.MD5.HashData(
            File.ReadAllBytes(Path.Combine(root.Path, ".specify", "memory", "constitution.md"))));
        string readme = File.ReadAllText(readmePath);
        Assert.Contains("Keep me.", readme);
        Assert.Contains("## SpecKit Constitution", readme);
        Assert.Contains("- File: `.specify/memory/constitution.md`", readme);
        Assert.Contains($"- MD5: `{expectedHash}`", readme);
        Assert.Contains("first import", console.SelectTitles.Last());
    }

    [Fact]
    public void RunImportSpecKitConstitutionDetectsChangedConstitutionAndReplacesRecordedHash()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        WriteConstitution(root.Path);
        FakeConsoleInteraction first = new([], ["Project Management", "Import SpecKit Constitution", "Exit"]);
        FakeRepositoryDocumentAgent agent = new();
        DevCraftMenuCommand.Run(context, first, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher(), documentAgent: agent);
        string readmePath = Path.Combine(root.Path, ".devcraft", "README.md");
        string firstReadme = File.ReadAllText(readmePath);

        FakeConsoleInteraction alreadyImported = new([], ["Project Management", "Import SpecKit Constitution", "No", "Exit"]);
        DevCraftMenuCommand.Run(context, alreadyImported, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher(), documentAgent: agent);
        Assert.Single(agent.Prompts);
        Assert.Contains(alreadyImported.SelectTitles, title => title.Contains("already been imported", StringComparison.Ordinal));
        Assert.Equal(["No", "Yes"], alreadyImported.SelectChoices[2]);
        Assert.StartsWith("The SpecKit constitution is already imported", alreadyImported.SelectTitles.Last(), StringComparison.Ordinal);

        FakeConsoleInteraction forced = new([], ["Project Management", "Import SpecKit Constitution", "Yes", "Exit"]);
        DevCraftMenuCommand.Run(context, forced, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher(), documentAgent: agent);
        Assert.Contains("unchanged since the last import", forced.SelectTitles.Last());
        Assert.Contains("unchanged since the last import", agent.Prompts[1]);
        Assert.Equal(firstReadme, File.ReadAllText(readmePath));

        File.AppendAllText(Path.Combine(root.Path, ".specify", "memory", "constitution.md"), "## Principle II\n");
        FakeConsoleInteraction changed = new([], ["Project Management", "Import SpecKit Constitution", "Exit"]);
        DevCraftMenuCommand.Run(context, changed, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher(), documentAgent: agent);

        Assert.Contains("changed since the last import", changed.SelectTitles.Last());
        Assert.Contains("has changed since the last import", agent.Prompts[2]);
        string readme = File.ReadAllText(readmePath);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(readme, "## SpecKit Constitution"));
        Assert.NotEqual(firstReadme, readme);
    }

    [Fact]
    public void RunImportSpecKitConstitutionFailureDoesNotRecordHash()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        WriteConstitution(root.Path);
        FakeConsoleInteraction console = new([], ["Project Management", "Import SpecKit Constitution", "Exit"]);

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher(), documentAgent: new FakeRepositoryDocumentAgent { Response = "nope" });

        string readmePath = Path.Combine(root.Path, ".devcraft", "README.md");
        Assert.False(File.Exists(readmePath) && File.ReadAllText(readmePath).Contains("## SpecKit Constitution"));
    }

    private static void WriteConstitution(string repositoryPath)
    {
        string memory = Path.Combine(repositoryPath, ".specify", "memory");
        Directory.CreateDirectory(memory);
        File.WriteAllText(Path.Combine(memory, "constitution.md"), "# Constitution\n\n## Principle I\n");
    }

    [Fact]
    public void NewFeature_AsksWorkItemIdAndUrl()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        FakeConsoleInteraction console = new([], ["Features", "Create new feature", "OpenAI Codex (codex)", "Exit"]);
        console.EnqueueFieldEdit(NewFeatureForm("Linked Feature", " PROJ-482 ", " https://jira.example.com/browse/PROJ-482 "));

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        DevCraftFeature feature = Assert.Single(Assert.Single(SystemCentralProjectsStore.Read(context.ProfileDirectory).Projects).Features);
        Assert.Equal("PROJ-482", feature.WorkItemId);
        Assert.Equal("https://jira.example.com/browse/PROJ-482", feature.ExternalReference);
    }

    [Fact]
    public void NewFeature_EmptyValues_StoredAsNull()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        FakeConsoleInteraction console = new([], ["Features", "Create new feature", "OpenAI Codex (codex)", "Exit"]);
        console.EnqueueFieldEdit(NewFeatureForm("Plain Feature", "   ", ""));

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        DevCraftFeature feature = Assert.Single(Assert.Single(SystemCentralProjectsStore.Read(context.ProfileDirectory).Projects).Features);
        Assert.Null(feature.WorkItemId);
        Assert.Null(feature.ExternalReference);
    }

    [Fact]
    public void NewFeature_InvalidUrl_ReopensFormPrefilled()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        FakeConsoleInteraction console = new([], ["Features", "Create new feature", "OpenAI Codex (codex)", "Exit"]);
        console.EnqueueFieldEdit(NewFeatureForm("Linked Feature", "1234", "dev.azure.com/org/p/_workitems/edit/1234"));
        console.EnqueueFieldEdit(NewFeatureForm("Linked Feature", "1234", "https://dev.azure.com/org/p/_workitems/edit/1234"));

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.Equal(2, console.FieldEditTitles.Count);
        Assert.Contains("Work Item URL must be an absolute http or https URL.", console.FieldEditTitles[1], StringComparison.Ordinal);
        Assert.Equal(
            ["Linked Feature", "1234", "dev.azure.com/org/p/_workitems/edit/1234"],
            console.FieldEditFields[1].Select(field => field.InitialValue));
        Assert.Equal(
            "https://dev.azure.com/org/p/_workitems/edit/1234",
            Assert.Single(Assert.Single(SystemCentralProjectsStore.Read(context.ProfileDirectory).Projects).Features).ExternalReference);
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("PROJ-482")]
    [InlineData("AB#1234")]
    [InlineData("#57")]
    public void NewFeature_FreeTextIds_Accepted(string workItemId)
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        FakeConsoleInteraction console = new([], ["Features", "Create new feature", "OpenAI Codex (codex)", "Exit"]);
        console.EnqueueFieldEdit(NewFeatureForm("Any Tracker", workItemId));

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.Equal(workItemId, Assert.Single(Assert.Single(SystemCentralProjectsStore.Read(context.ProfileDirectory).Projects).Features).WorkItemId);
    }

    [Fact]
    public void RemoveWorkingCopy_Confirm_Removes()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        string other = Path.Combine(root.Path, "Active Work 1", "ProjectCodeArc");
        Directory.CreateDirectory(other);
        WriteCopies(context, new WorkingCopy(other, "story/1", new DateTimeOffset(2026, 10, 15, 8, 30, 0, TimeSpan.Zero)), new WorkingCopy(root.Path, "main", null));
        string choice = $"Active Work 1 — {other} — story/1 — last seen 2026-10-15 08:30 UTC";
        FakeConsoleInteraction console = new([], ["Project Management", "Remove Working Copy", choice, "Yes", "Exit"]);

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.Contains(choice, console.SelectChoices[console.SelectTitles.IndexOf("Remove Working Copy")]);
        Assert.Contains($"Remove working copy {other}?", console.SelectTitles);
        Assert.Equal([root.Path], CopyPaths(context));
        Assert.StartsWith($"Removed working copy Active Work 1 ({other}).", console.SelectTitles.Last(), StringComparison.Ordinal);
    }

    [Fact]
    public void RemoveWorkingCopy_Decline_Keeps()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        string other = Path.Combine(root.Path, "Active Work 1", "ProjectCodeArc");
        Directory.CreateDirectory(other);
        WriteCopies(context, new WorkingCopy(other, null, null));
        FakeConsoleInteraction console = new([], ["Project Management", "Remove Working Copy", $"Active Work 1 — {other} — (none) — last seen unknown", "No", "Exit"]);

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.Equal([other], CopyPaths(context));
    }

    [Fact]
    public void RemoveWorkingCopy_CurrentCopy_Refused()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        WriteCopies(context, new WorkingCopy(root.Path, "main", null));
        FakeConsoleInteraction console = new([], ["Project Management", "Remove Working Copy", $"{Path.GetFileName(root.Path)} — {root.Path} — main — last seen unknown", "Exit"]);

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.Equal([root.Path], CopyPaths(context));
        Assert.StartsWith("This is the working copy DevCraft is running in; it cannot be removed.", console.SelectTitles.Last(), StringComparison.Ordinal);
    }

    [Fact]
    public void RemoveWorkingCopy_MissingFolder_Flagged()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        string gone = Path.Combine(root.Path, "Active Work 3", "ProjectCodeArc");
        WriteCopies(context, new WorkingCopy(gone, "story/3", null));
        FakeConsoleInteraction console = new([], ["Project Management", "Remove Working Copy", "Back", "Exit"]);

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.Contains(
            $"Active Work 3 — {gone} — story/3 — last seen unknown — missing",
            console.SelectChoices[console.SelectTitles.IndexOf("Remove Working Copy")]);
    }

    [Fact]
    public void RemoveWorkingCopy_NoCopies_ExplainsAndReturns()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        FakeConsoleInteraction console = new([], ["Project Management", "Remove Working Copy", "Exit"]);

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.StartsWith("No working copies are recorded for this project yet.", console.SelectTitles.Last(), StringComparison.Ordinal);
    }

    private static void WriteCopies(StartupContext context, params WorkingCopy[] copies)
    {
        ProjectDevCraftConfiguration configuration = ProjectDevCraftConfigurationReader.Read(context.CurrentDirectory)!;
        SystemCentralProjectsStore.Write(
            context.ProfileDirectory,
            new SystemCentralProjectsIndex([new SystemCentralProject(configuration.ProjectKey, "Code Arc", "code-arc", "ProjectCodeArc", null, copies, [])]));
    }

    private static List<string> CopyPaths(StartupContext context)
    {
        return Assert.Single(SystemCentralProjectsStore.Read(context.ProfileDirectory).Projects).WorkingCopies.Select(copy => copy.Path).ToList();
    }

    private static Dictionary<string, string> NewFeatureForm(string name, string workItemId = "", string workItemUrl = "")
    {
        return new Dictionary<string, string>
        {
            ["FeatureName"] = name,
            ["WorkItemId"] = workItemId,
            ["WorkItemUrl"] = workItemUrl,
        };
    }

    private static StartupContext MenuContext(TestDirectory root, TestDirectory profileRoot)
    {
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProfileStructureInitializer.Ensure(profile);
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);

        return new StartupContext(root.Path, profile, Path.Combine(profile, "soul.md"));
    }

    [Fact]
    public void RunOnlyShowsAvailableAiClients()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProfileStructureInitializer.Ensure(profile);
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Project Management", "Project discovery", "OpenAI Codex (codex)", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        int clientPromptIndex = console.SelectTitles.FindIndex(title => title == "Which AI client should I use?");
        IReadOnlyList<string> choices = console.SelectChoices[clientPromptIndex];
        string choice = Assert.Single(choices);
        Assert.Equal("OpenAI Codex (codex)", choice);
    }

    [Fact]
    public void RunReturnsToMenuWhenAiClientFailsToStart()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProfileStructureInitializer.Ensure(profile);
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Project Management", "Project discovery", "OpenAI Codex (codex)", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new()
        {
            ExceptionToThrow = new InvalidOperationException("Could not start OpenAI Codex."),
        };
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        Assert.Contains(
            console.SelectTitles,
            title => title.Contains("Could not start the selected AI client", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RunMainMenuShowsProjectManagementInsteadOfProjectActions()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        IReadOnlyList<string> mainChoices = console.SelectChoices[0];
        Assert.Contains("Project Management", mainChoices);
        Assert.DoesNotContain("Project discovery", mainChoices);
        Assert.DoesNotContain("Project design", mainChoices);
        Assert.DoesNotContain("Project theme", mainChoices);
        Assert.DoesNotContain("Project setup", mainChoices);
        Assert.DoesNotContain("Create Project", mainChoices);
    }

    [Fact]
    public void RunProjectManagementShowsProjectActionsWithBack()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Project Management", "Back", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        int projectManagementIndex = console.SelectTitles.FindIndex(title => title == "Project Management");
        IReadOnlyList<string> choices = console.SelectChoices[projectManagementIndex];
        Assert.Equal(["Project discovery", "Project design", "Project theme", "Project setup", "Create Project", "Import SpecKit Constitution", "Infer Project Details", "Remove Working Copy", "Back"], choices);
        Assert.Contains("What do you want to work on?", console.SelectTitles.Last());
        Assert.Empty(launcher.Instructions);
        Assert.Empty(featureLauncher.Launches);
    }

    [Fact]
    public void RunProjectManagementReturnsToMainMenuAfterProjectHandoff()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Project Management", "Project design", "OpenAI Codex (codex)", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        string instruction = Assert.Single(launcher.Instructions);
        Assert.Contains("project design", instruction, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("What do you want to work on?", console.SelectTitles.Last());
        Assert.Empty(featureLauncher.Launches);
    }

    [Fact]
    public void RunFeatureMenuCreatesNewFeature()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Features", "Create new feature", "OpenAI Codex (codex)", "Exit"]);
        console.EnqueueFieldEdit(NewFeatureForm("Menu Feature"));
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        Assert.Empty(ProjectDevCraftConfigurationReader.Read(root.Path)!.Features);
        DevCraftFeature feature = Assert.Single(Assert.Single(SystemCentralProjectsStore.Read(profile).Projects).Features);
        Assert.Equal("Menu Feature", feature.Name);
        Assert.Empty(launcher.Instructions);
        Assert.Single(featureLauncher.Launches);
    }

    [Fact]
    public void RunNewFeatureUsesFeatureNameFormInsteadOfAsk()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        FakeConsoleInteraction console = new([], ["Features", "Create new feature", "OpenAI Codex (codex)", "Exit"]);
        console.EnqueueFieldEdit(NewFeatureForm("  Form Feature  "));

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.Empty(console.AskPrompts);
        Assert.Equal("New Feature", Assert.Single(console.FieldEditTitles));
        Assert.Equal(["Feature name", "Work Item ID", "Work Item URL"], console.FieldEditFields[0].Select(field => field.Label));
        Assert.All(console.FieldEditFields[0], field => Assert.Equal(string.Empty, field.InitialValue));
        Assert.True(console.FieldEditFields[0][0].Required);
        Assert.Equal("Form Feature", Assert.Single(Assert.Single(SystemCentralProjectsStore.Read(context.ProfileDirectory).Projects).Features).Name);
    }

    [Theory]
    [InlineData("Discarded", false)]
    [InlineData("   ", true)]
    public void RunNewFeatureCancelledOrBlankNameCreatesNothing(string name, bool saved)
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        FakeConsoleInteraction console = new([], ["Features", "Create new feature", "Exit"]);
        console.EnqueueFieldEdit(NewFeatureForm(name), saved);
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), featureLauncher);

        Assert.Empty(featureLauncher.Launches);
        Assert.Empty(ProjectDevCraftConfigurationReader.Read(root.Path)!.Features);
        Assert.DoesNotContain("Starting Prompt", console.TextEditTitles);
        Assert.StartsWith("Feature was not created.", console.SelectTitles.Last(), StringComparison.Ordinal);
    }

    [Fact]
    public void RunCreateProjectLaunchesWithSelectedProjectType()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        using TestDirectory sourceRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(profile, "project-types"));
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        File.WriteAllText(
            Path.Combine(profile, "project-types", "web-api.md"),
            """
            # Web API

            ## Purpose

            Create a service-oriented HTTP API.
            """);
        ProfileStructureInitializer.Ensure(profile, sourceRoot.Path);
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Project Management", "Create Project", "Web API (web-api)", "OpenAI Codex (codex)", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        string instruction = Assert.Single(launcher.Instructions);
        Assert.Contains("create a project", instruction, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Web API", instruction);
        Assert.Contains(Path.Combine(profile, "project-types", "web-api.md"), instruction);
        Assert.Empty(featureLauncher.Launches);
    }

    [Fact]
    public void RunCreateProjectWithoutProjectTypesReturnsToWorkMenu()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        using TestDirectory sourceRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProfileStructureInitializer.Ensure(profile, sourceRoot.Path);
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Project Management", "Create Project", "Project Management", "Project discovery", "OpenAI Codex (codex)", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        Assert.Contains(
            console.SelectTitles,
            title => title.Contains("There are no project types in the catalog", StringComparison.OrdinalIgnoreCase));
        string instruction = Assert.Single(launcher.Instructions);
        Assert.Contains("project discovery", instruction, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(featureLauncher.Launches);
    }

    [Fact]
    public void RunShowsSituationalConversationOnMainMenu()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProfileStructureInitializer.Ensure(profile);
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        Assert.Contains("Situational Conversation", console.SelectChoices[0]);
        Assert.Empty(launcher.Instructions);
        Assert.Empty(featureLauncher.Launches);
    }

    [Fact]
    public void RunSituationalConversationLaunchesSelectedAgentWithPlanningInstruction()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        DevCraftProfileConfigurationWriter.Write(profile, new DevCraftProfileConfiguration([], [], [], [], [], [], [], true));
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Situational Conversation", "OpenAI Codex (codex)", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        (SupportedTerminalClient client, string instruction) = Assert.Single(launcher.Launches);
        Assert.Equal("codex", client.Slug);
        Assert.Contains("situational conversation", instruction, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("plan the day", instruction, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("remember past context", instruction, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ask the user what they want to accomplish", instruction, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("projects.json", instruction, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("update tracked project and feature information", instruction, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Discovery, Clarification, Research, Planning, Analysis, Implementation, and Complete", instruction, StringComparison.Ordinal);
        Assert.Contains("does not authorize implementation", instruction, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("project discovery", instruction, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("working-copies", instruction, StringComparison.Ordinal);
        Assert.Contains("repo-remote", instruction, StringComparison.Ordinal);
        Assert.Contains("branch", instruction, StringComparison.Ordinal);
        Assert.DoesNotContain("repo-location", instruction, StringComparison.Ordinal);
        Assert.Empty(featureLauncher.Launches);
    }

    [Fact]
    public void RunSituationalConversationWhenDisabledReturnsConfigurationNotice()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProfileStructureInitializer.Ensure(profile);
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Situational Conversation", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        Assert.Contains(
            console.SelectTitles,
            title => title.Contains("Situational awareness is currently disabled", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(launcher.Instructions);
        Assert.Empty(featureLauncher.Launches);
    }

    [Fact]
    public void RunConfigureDevCraftImportSettingsLaunchesImportSkill()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        using TestDirectory importRoot = new();
        using TestDirectory sourceRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProfileStructureInitializer.Ensure(profile, sourceRoot.Path);
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([importRoot.Path], ["Configure DevCraft", "Import Settings", "OpenAI Codex (codex)", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        string instruction = Assert.Single(launcher.Instructions);
        Assert.Contains("Import DevCraft settings", instruction);
        Assert.Contains(importRoot.Path, instruction);
        Assert.Contains(Path.Combine(profile, "skills", "DevCraft-Import.md"), instruction);
        Assert.Empty(featureLauncher.Launches);
    }

    [Fact]
    public void RunConfigureDevCraftCreateSkillLaunchesCreationSkill()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        using TestDirectory sourceRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProfileStructureInitializer.Ensure(profile, sourceRoot.Path);
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Configure DevCraft", "Create Skill", "OpenAI Codex (codex)", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        string instruction = Assert.Single(launcher.Instructions);
        Assert.Contains("Create Skill", instruction);
        Assert.Contains(Path.Combine(profile, "skills", "Create-Skill.md"), instruction);
        Assert.Contains(Path.Combine(profile, "templates", "skill-template.md"), instruction);
        Assert.Contains("Read the three core DevCraft files", instruction);
        Assert.Contains("ask the user where it should be created", instruction);
        Assert.Contains("Current repository", instruction);
        Assert.Contains("Profile DevCraft folder", instruction);
        Assert.Contains(Path.Combine(profile, "configure.json"), instruction);
        Assert.Contains(ProjectDevCraftConfigurationStore.PathFor(root.Path), instruction);
        Assert.Empty(featureLauncher.Launches);
    }

    [Fact]
    public void RunConfigureDevCraftChangeRepositoryLevelFeatureStorageUpdatesRepositoryOnly()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        using TestDirectory sourceRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProfileStructureInitializer.Ensure(profile, sourceRoot.Path);
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Configure DevCraft", "Change Repository-Level Feature Storage", "system central (system-central)", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        ProjectDevCraftConfiguration repositoryConfiguration = ProjectDevCraftConfigurationReader.Read(root.Path)!;
        DevCraftProfileConfiguration profileConfiguration = ProfileConfigurationReader.Read(profile);
        Assert.Equal("system-central", repositoryConfiguration.SelectedFeatureStorage);
        Assert.DoesNotContain("\"SelectedFeatureStorage\"", File.ReadAllText(Path.Combine(profile, "configure.json")));
        Assert.Contains("Change Repository-Level Feature Storage", console.SelectChoices.SelectMany(choice => choice));
        Assert.DoesNotContain("Change Feature Storage", console.SelectChoices.SelectMany(choice => choice));
        Assert.Empty(launcher.Instructions);
        Assert.Empty(featureLauncher.Launches);
    }

    [Fact]
    public void RunConfigureDevCraftChangeRepositoryLevelFeatureStorageCanClearOverride()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        using TestDirectory sourceRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProfileStructureInitializer.Ensure(profile, sourceRoot.Path);
        ProjectDevCraftConfigurationStore.Write(
            root.Path,
            new ProjectDevCraftConfiguration(
                Guid.NewGuid().ToString(),
                FeatureStorageMode.RepoCentral,
                "features.json",
                SampleProfile,
                []));
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Configure DevCraft", "Change Repository-Level Feature Storage", "Use Profile Default", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        ProjectDevCraftConfiguration repositoryConfiguration = ProjectDevCraftConfigurationReader.Read(root.Path)!;
        Assert.Null(repositoryConfiguration.SelectedFeatureStorage);
        Assert.Contains(console.StatusMessages, message => message.Contains("profile default", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(launcher.Instructions);
        Assert.Empty(featureLauncher.Launches);
    }

    [Fact]
    public void RunConfigureDevCraftBackReturnsToMainMenu()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        using TestDirectory sourceRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProfileStructureInitializer.Ensure(profile, sourceRoot.Path);
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Configure DevCraft", "Back", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        Assert.Contains("Configure DevCraft", console.SelectTitles);
        int configureIndex = console.SelectTitles.FindIndex(title => title == "Configure DevCraft");
        Assert.Contains("Back", console.SelectChoices[configureIndex]);
        Assert.Empty(launcher.Instructions);
        Assert.Empty(featureLauncher.Launches);
    }

    [Fact]
    public void RunConfigureGitToolingOpensGitToolingMenu()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = MenuContext(root, profileRoot);
        FakeConsoleInteraction console = new([], ["Configure DevCraft", "Configure GIT Tooling", "Setup Branch Template", "Back", "Exit"]);
        console.EnqueueValueEdit("{adUser}/{FeatureType}/{WorkItem}-{Title}");

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher(), gitRunner: new FakeGitRunner());

        int configureIndex = console.SelectTitles.FindIndex(title => title == "Configure DevCraft");
        Assert.Contains("Configure GIT Tooling", console.SelectChoices[configureIndex]);
        Assert.Equal("{adUser}/{FeatureType}/{WorkItem}-{Title}", ProfileConfigurationReader.Read(context.ProfileDirectory).GitTooling.BranchTemplate);
    }

    [Fact]
    public void RunConfigureSituationalAwarenessDatabaseShowsMongoDockerInstructions()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        using TestDirectory sourceRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProfileStructureInitializer.Ensure(profile, sourceRoot.Path);
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new(
            [],
            ["Configure DevCraft", "Configure Situational Awareness", "Yes", "Months & Weeks", "Database", "Exit"]);
        console.EnqueueValueEdit("mongodb://localhost:27017/DevCraft");
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        DevCraftProfileConfiguration configuration = ProfileConfigurationReader.Read(profile);
        Assert.True(configuration.SituationEnabled);
        Assert.Equal("weeks", configuration.SituationScale);
        Assert.Equal("database", configuration.SituationStorage);
        Assert.Equal("mongodb://localhost:27017/DevCraft", configuration.SituationConnection);
        Assert.Contains(console.StatusMessages, message => message.Contains("docker run --name devcraft-mongo", StringComparison.Ordinal));
    }

    [Fact]
    public void RunManagePeopleAddsPersonAndReturnsToManagePeopleMenu()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        DevCraftProfileConfigurationWriter.Write(profile, new DevCraftProfileConfiguration([], [], [], [], [], [], [], true));
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new(
            ["Ada", "Lovelace", "ada@example.com", "555-0100", "Principal Engineer", "Platform", "Analytical Engines"],
            ["Logging", "Manage People", "Add Person", "ACTIVE", "Back", "Exit"]);
        console.EnqueueTextEdit("Trusted collaborator");
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        SituationPerson person = Assert.Single(new FileSituationStore(profile).Read(false).People);
        Assert.Equal("Ada", person.FirstName);
        Assert.Equal("Principal Engineer", person.JobTitle);
        Assert.Equal("Platform", person.AssignedTeam);
        Assert.Equal("Analytical Engines", person.Organization);
        Assert.Equal("Trusted collaborator", person.Relation);
        Assert.Equal("ACTIVE", person.Status);
        Assert.Contains("Manage People", console.SelectTitles);
        Assert.Equal(2, console.SelectTitles.Count(title => title == "Manage People"));
    }

    [Fact]
    public void RunManagePeopleListsPeopleSortedWithGoBackFirst()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        DevCraftProfileConfigurationWriter.Write(profile, new DevCraftProfileConfiguration([], [], [], [], [], [], [], true));
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        FileSituationStore store = new(profile);
        store.AddPerson(new SituationPerson("person-2", "Grace", "Hopper", "grace@example.com", "555-0101", "Mentor", "ACTIVE", "Admiral", "Compiler", "Navy", null));
        store.AddPerson(new SituationPerson("person-1", "Ada", "Lovelace", "ada@example.com", "555-0100", "Advisor", "ACTIVE", "Architect", "Math", "Analytical Engines", null));
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Logging", "Manage People", "List People", "Go Back", "Back", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        int listIndex = console.SelectTitles.FindIndex(title => title == "List People");
        Assert.Equal("Go Back", console.SelectChoices[listIndex][0]);
        Assert.Equal("Ada Lovelace (ada@example.com) | Architect | Math | Analytical Engines", console.SelectChoices[listIndex][1]);
        Assert.Equal("Grace Hopper (grace@example.com) | Admiral | Compiler | Navy", console.SelectChoices[listIndex][2]);
    }

    [Fact]
    public void RunManagePeopleEditsPersonAndTogglesInactiveDate()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        DevCraftProfileConfigurationWriter.Write(profile, new DevCraftProfileConfiguration([], [], [], [], [], [], [], true));
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        FileSituationStore store = new(profile);
        store.AddPerson(new SituationPerson("person-1", "Ada", "Lovelace", "ada@example.com", "555-0100", "Advisor", "ACTIVE", "Architect", "Math", "Analytical Engines", null));
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new(
            ["Ada", "Byron"],
            ["Logging", "Manage People", "List People", "Ada Lovelace (ada@example.com) | Architect | Math | Analytical Engines", "Edit Name", "Make Inactive", "Go Back", "Go Back", "Back", "Exit"]);
        console.EnqueueValueEdit("2/2/2026");
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        SituationPerson person = Assert.Single(new FileSituationStore(profile).Read(false).People);
        Assert.Equal("person-1", person.RowId);
        Assert.Equal("Ada", person.FirstName);
        Assert.Equal("Byron", person.LastName);
        Assert.Equal("INACTIVE", person.Status);
        Assert.Equal(new DateTimeOffset(2026, 2, 2, 0, 0, 0, TimeSpan.Zero), person.InactiveDate);
        Assert.Contains(console.StatusMessages, message => message.Contains("INACTIVE (2/2/2026)", StringComparison.Ordinal));
    }

    [Fact]
    public void ManagePeopleMakeInactivePrefillsTodayWhenNoDate()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        (StartupContext context, string profile) = CreateSituationContext(root, profileRoot);
        new FileSituationStore(profile).AddPerson(new SituationPerson(
            "person-1", "Ada", "Lovelace", "ada@example.com", "555-0100", "Advisor", "ACTIVE", "Architect", "Math", "Analytical Engines", null));
        FakeConsoleInteraction console = new(
            [],
            ["Logging", "Manage People", "List People", "Ada Lovelace (ada@example.com) | Architect | Math | Analytical Engines", "Make Inactive", "Go Back", "Go Back", "Back", "Exit"]);

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.Equal([DateTime.Today.ToString("yyyy-MM-dd")], console.ValueEditInitialValues);
        SituationPerson person = Assert.Single(new FileSituationStore(profile).Read(false).People);
        Assert.Equal("INACTIVE", person.Status);
        Assert.Equal(new DateTimeOffset(DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified), TimeSpan.Zero), person.InactiveDate);
    }

    [Fact]
    public void RunManagePeopleMakesInactivePersonActiveAndClearsInactiveDate()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        DevCraftProfileConfigurationWriter.Write(profile, new DevCraftProfileConfiguration([], [], [], [], [], [], [], true));
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        FileSituationStore store = new(profile);
        store.AddPerson(new SituationPerson(
            "person-1",
            "Ada",
            "Byron",
            "ada@example.com",
            "555-0100",
            "Advisor",
            "INACTIVE",
            "Architect",
            "Math",
            "Analytical Engines",
            new DateTimeOffset(2026, 2, 2, 0, 0, 0, TimeSpan.Zero)));
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new(
            [],
            ["Logging", "Manage People", "List People", "Ada Byron (ada@example.com) | Architect | Math | Analytical Engines", "Make Active", "Go Back", "Go Back", "Back", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        SituationPerson person = Assert.Single(new FileSituationStore(profile).Read(false).People);
        Assert.Equal("person-1", person.RowId);
        Assert.Equal("ACTIVE", person.Status);
        Assert.Null(person.InactiveDate);
    }

    [Fact]
    public void RunManageLogsEditsUncompressedLogEntryAndPreservesIdentity()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        DevCraftProfileConfigurationWriter.Write(profile, new DevCraftProfileConfiguration([], [], [], [], [], [], [], true));
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        FileSituationStore store = new(profile);
        store.AddLogEntry(new SituationLogEntry("log-1", DateTimeOffset.Parse("2026-10-09T08:00:00-04:00"), "Original", false));
        store.AddLogEntry(new SituationLogEntry("log-2", DateTimeOffset.Parse("2026-10-09T07:00:00-04:00"), "Compressed", true));
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new(
            [],
            ["Logging", "Manage Logs", "List Log Entries", "2026-10-09 08:00 | Original", "Edit", "Go Back", "Go Back", "Back", "Exit"]);
        console.EnqueueTextEdit("Updated\nMarkdown");
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        SituationSnapshot snapshot = new FileSituationStore(profile).Read(false);
        SituationLogEntry updated = Assert.Single(snapshot.LogEntries.Where(entry => entry.RowId == "log-1"));
        Assert.Equal("Updated\nMarkdown", updated.LogData);
        Assert.Equal(DateTimeOffset.Parse("2026-10-09T08:00:00-04:00"), updated.DateTime);
        Assert.Empty(console.AskPrompts);
        Assert.Empty(console.ValueEditTitles);
        Assert.False(updated.IsCompressed);
        Assert.DoesNotContain(console.SelectChoices.SelectMany(choice => choice), choice => choice.Contains("Compressed", StringComparison.Ordinal));
        Assert.Contains(console.TextEditTitles, title => title == "Edit Log Entry Content");
    }

    [Fact]
    public void RunManageLogsCancelEditDoesNotWriteSummary()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        DevCraftProfileConfigurationWriter.Write(profile, new DevCraftProfileConfiguration([], [], [], [], [], [], [], true));
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        FileSituationStore store = new(profile);
        store.AddSummary(new SituationSummary("summary-1", DateTimeOffset.Parse("2026-10-09T08:00:00-04:00"), "Original", "week", false));
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new(
            [],
            ["Logging", "Manage Logs", "List Summaries", "Week", "2026-10-09 08:00 | Original", "Edit", "Go Back", "Go Back", "Go Back", "Back", "Exit"]);
        console.EnqueueTextEdit("Updated", saved: false);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        SituationSummary summary = Assert.Single(new FileSituationStore(profile).Read(false).Summaries);
        Assert.Equal("Original", summary.SummaryData);
        Assert.Equal(DateTimeOffset.Parse("2026-10-09T08:00:00-04:00"), summary.DateTime);
        Assert.Equal("week", summary.Type);
        Assert.False(summary.IsCompressed);
    }

    [Fact]
    public void RunManageLogsEditOpensContentWithExistingTextAndNoDatePrompt()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        (StartupContext context, string profile) = CreateSituationContext(root, profileRoot);
        new FileSituationStore(profile).AddLogEntry(new SituationLogEntry("log-1", DateTimeOffset.Parse("2026-10-09T08:00:00-04:00"), "Original", false));
        FakeConsoleInteraction console = new(
            [],
            ["Logging", "Manage Logs", "List Log Entries", "2026-10-09 08:00 | Original", "Edit", "Go Back", "Go Back", "Back", "Exit"]);

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.Equal(["Original"], console.TextEditInitialTexts);
        Assert.Empty(console.AskPrompts);
        Assert.Empty(console.ValueEditTitles);
        SituationLogEntry entry = Assert.Single(new FileSituationStore(profile).Read(false).LogEntries);
        Assert.Equal("Original", entry.LogData);
        Assert.Equal(DateTimeOffset.Parse("2026-10-09T08:00:00-04:00"), entry.DateTime);
    }

    [Fact]
    public void RunManageSummariesEditPrefillsDateAndContent()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        (StartupContext context, string profile) = CreateSituationContext(root, profileRoot);
        DateTimeOffset original = DateTimeOffset.Parse("2026-10-09T08:00:00-04:00");
        new FileSituationStore(profile).AddSummary(new SituationSummary("summary-1", original, "Original", "week", false));
        FakeConsoleInteraction console = new([], SummaryEditSelections);
        console.EnqueueValueEdit("2026-10-09T09:00:00-04:00");
        console.EnqueueTextEdit("Updated");

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.Equal([original.ToString("u")], console.ValueEditInitialValues);
        Assert.Equal(["Original"], console.TextEditInitialTexts);
        Assert.Empty(console.AskPrompts);
        SituationSummary summary = Assert.Single(new FileSituationStore(profile).Read(false).Summaries);
        Assert.Equal("Updated", summary.SummaryData);
        Assert.Equal(DateTimeOffset.Parse("2026-10-09T09:00:00-04:00"), summary.DateTime);
    }

    [Fact]
    public void RunManageSummariesEditKeepsDateWhenAcceptedUnchanged()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        (StartupContext context, string profile) = CreateSituationContext(root, profileRoot);
        DateTimeOffset original = DateTimeOffset.Parse("2026-10-09T08:00:00-04:00");
        new FileSituationStore(profile).AddSummary(new SituationSummary("summary-1", original, "Original", "week", false));
        FakeConsoleInteraction console = new([], SummaryEditSelections);
        console.EnqueueTextEdit("Updated");

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        SituationSummary summary = Assert.Single(new FileSituationStore(profile).Read(false).Summaries);
        Assert.Equal("Updated", summary.SummaryData);
        Assert.Equal(original, summary.DateTime);
    }

    [Fact]
    public void RunManageSummariesEditInvalidDateDoesNotChangeSummary()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        (StartupContext context, string profile) = CreateSituationContext(root, profileRoot);
        DateTimeOffset original = DateTimeOffset.Parse("2026-10-09T08:00:00-04:00");
        new FileSituationStore(profile).AddSummary(new SituationSummary("summary-1", original, "Original", "week", false));
        FakeConsoleInteraction console = new([], SummaryEditSelections);
        console.EnqueueValueEdit("not a date");
        console.EnqueueTextEdit("Updated");

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        SituationSummary summary = Assert.Single(new FileSituationStore(profile).Read(false).Summaries);
        Assert.Equal("Original", summary.SummaryData);
        Assert.Equal(original, summary.DateTime);
        Assert.Contains("Summary was not changed.", console.StatusMessages);
    }

    private static readonly string[] SummaryEditSelections =
        ["Logging", "Manage Logs", "List Summaries", "Week", "2026-10-09 08:00 | Original", "Edit", "Go Back", "Go Back", "Go Back", "Back", "Exit"];

    private static (StartupContext Context, string Profile) CreateSituationContext(TestDirectory root, TestDirectory profileRoot)
    {
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        DevCraftProfileConfigurationWriter.Write(profile, new DevCraftProfileConfiguration([], [], [], [], [], [], [], true));
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);

        return (new StartupContext(root.Path, profile, Path.Combine(profile, "soul.md")), profile);
    }

    [Fact]
    public void RunConfigureDevCraftEditsSupportedClientWithoutNormalizingAwayManualValues()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        SupportedTerminalClient client = new(
            "custom-agent",
            "Custom Agent",
            "Original description",
            new TerminalClientOperation("Original scan", "/opt/custom-scan", ["scan", "{prompt}"]),
            new TerminalClientOperation("Original session", "/opt/custom-session", ["session", "{prompt}"]));
        DevCraftProfileConfigurationWriter.Write(profile, new DevCraftProfileConfiguration([], [], [], [], [], [], [client]));
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new(
            ["Updated Agent", "custom-agent", "/opt/custom-scan-v2", "/opt/custom-session-v2"],
            ["Configure DevCraft", "Configure Supported Clients", "Custom Agent (custom-agent)", "Edit", "Back", "Exit"]);
        console.EnqueueTextEdit("Updated description");
        console.EnqueueTextEdit("Updated scan description");
        console.EnqueueTextEdit("scan\n--json\n{prompt}");
        console.EnqueueTextEdit("Updated session description");
        console.EnqueueTextEdit("session\n{prompt}");
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        SupportedTerminalClient updated = Assert.Single(ProfileConfigurationReader.Read(profile).SupportedClients.Where(item => item.Slug == "custom-agent"));
        Assert.Equal("Updated Agent", updated.Name);
        Assert.Equal("Updated description", updated.Description);
        Assert.Equal("/opt/custom-scan-v2", updated.Scan.BinaryPath);
        Assert.Equal(["scan", "--json", "{prompt}"], updated.Scan.Arguments);
        Assert.Equal("Updated session description", updated.Session.Description);
        Assert.Equal("/opt/custom-session-v2", updated.Session.BinaryPath);
        Assert.Equal(["session", "{prompt}"], updated.Session.Arguments);
    }

    [Fact]
    public void RunFeatureMenuBackReturnsToMainMenu()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Features", "Back", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        Assert.Contains("Feature options", console.SelectTitles);
        int featureIndex = console.SelectTitles.FindIndex(title => title == "Feature options");
        Assert.Contains("Back", console.SelectChoices[featureIndex]);
        Assert.Empty(launcher.Instructions);
        Assert.Empty(featureLauncher.Launches);
    }

    [Fact]
    public void RunFeatureListBackDoesNotPromptForAiClient()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        DevCraftFeature feature = new(
            "Menu Feature",
            "menu-feature",
            "Validate feature list back behavior.",
            "feature-folder",
            FeatureStorageMode.RepoCentral,
            null);
        ProjectDevCraftConfigurationStore.Write(
            root.Path,
            new ProjectDevCraftConfiguration(
                Guid.NewGuid().ToString(),
                null,
                "features.json",
                SampleProfile,
                [feature]));
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Features", "List features", "Back", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        int featureListIndex = console.SelectTitles.FindIndex(title => title == "Select a feature");
        Assert.True(featureListIndex >= 0);
        Assert.Contains("Back", console.SelectChoices[featureListIndex]);
        Assert.DoesNotContain("Which AI client should I use?", console.SelectTitles);
        Assert.Empty(featureLauncher.Launches);
    }

    [Fact]
    public void RunFeatureListPromptsForAiClientAfterFeatureSelection()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProfileStructureInitializer.Ensure(profile);
        DevCraftFeature feature = new(
            "Menu Feature",
            "menu-feature",
            "Validate deferred client selection.",
            "feature-folder",
            FeatureStorageMode.RepoCentral,
            null);
        ProjectDevCraftConfigurationStore.Write(
            root.Path,
            new ProjectDevCraftConfiguration(
                Guid.NewGuid().ToString(),
                null,
                "features.json",
                SampleProfile,
                [feature]));
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Features", "List features", "Menu Feature (menu-feature)", "Start a conversation about the feature", "OpenAI Codex (codex)", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        int featureListIndex = console.SelectTitles.FindIndex(title => title == "Select a feature");
        int clientPromptIndex = console.SelectTitles.FindIndex(title => title == "Which AI client should I use?");
        Assert.True(featureListIndex >= 0);
        Assert.True(clientPromptIndex > featureListIndex);
        Assert.Single(featureLauncher.Launches);
        Assert.Equal("codex", featureLauncher.Launches[0].Client.Slug);
    }

    [Fact]
    public void RunCreateProjectBackReturnsToMainMenu()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        using TestDirectory sourceRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(profile, "project-types"));
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        File.WriteAllText(
            Path.Combine(profile, "project-types", "web-api.md"),
            """
            # Web API

            ## Purpose

            Create a service-oriented HTTP API.
            """);
        ProfileStructureInitializer.Ensure(profile, sourceRoot.Path);
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Project Management", "Create Project", "Back", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        Assert.Contains("Select a project type", console.SelectTitles);
        int projectTypeIndex = console.SelectTitles.FindIndex(title => title == "Select a project type");
        Assert.Contains("Back", console.SelectChoices[projectTypeIndex]);
        Assert.Empty(launcher.Instructions);
        Assert.Empty(featureLauncher.Launches);
    }

    [Fact]
    public void RunSkillsMenuLaunchesSelectedSkill()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        using TestDirectory sourceRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProfileStructureInitializer.Ensure(profile, sourceRoot.Path);
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Skills", "Create Skill (create-skill)", "OpenAI Codex (codex)", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        string instruction = Assert.Single(launcher.Instructions);
        Assert.Contains("Use the selected DevCraft skill", instruction);
        Assert.Contains("Create Skill", instruction);
        Assert.Contains("create-skill", instruction);
        Assert.Contains(Path.Combine(profile, "skills", "Create-Skill.md"), instruction);
        Assert.Contains("ask the user for the details required by that skill", instruction);
        Assert.Empty(featureLauncher.Launches);
    }

    [Fact]
    public void RunSkillsMenuBackReturnsToMainMenu()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        using TestDirectory sourceRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProfileStructureInitializer.Ensure(profile, sourceRoot.Path);
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Skills", "Back", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        Assert.Contains("Select a skill", console.SelectTitles);
        int skillIndex = console.SelectTitles.FindIndex(title => title == "Select a skill");
        Assert.Contains("Back", console.SelectChoices[skillIndex]);
        Assert.Empty(launcher.Instructions);
        Assert.Empty(featureLauncher.Launches);
    }

    [Fact]
    public void RunAddDevCraftToDesktopAgentShowsBuiltInstructions()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = CreateDesktopAgentProfile(root, profileRoot, situationEnabled: true);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Configure DevCraft", "Add DevCraft To Desktop Agent", "Exit"]);

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.Contains("Copy and paste this text into your desktop agent's instructions:", console.StatusMessages);
        Assert.Contains(DesktopAgentInstructionsBuilder.Build(profile), console.StatusMessages);
    }

    [Fact]
    public void RunAddDevCraftToDesktopAgentReflectsCurrentConfiguration()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = CreateDesktopAgentProfile(root, profileRoot, situationEnabled: false);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction first = new([], ["Configure DevCraft", "Add DevCraft To Desktop Agent", "Exit"]);
        FakeConsoleInteraction second = new([], ["Configure DevCraft", "Add DevCraft To Desktop Agent", "Exit"]);

        DevCraftMenuCommand.Run(context, first, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());
        DevCraftProfileConfigurationWriter.Write(profile, new DevCraftProfileConfiguration([], [], [], [], [], [], [], true));
        DevCraftMenuCommand.Run(context, second, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.DoesNotContain(first.StatusMessages, message => message.Contains("Situational Awareness:", StringComparison.Ordinal));
        Assert.Contains(second.StatusMessages, message => message.Contains("Situational Awareness:", StringComparison.Ordinal));
    }

    [Fact]
    public void RunAddDevCraftToDesktopAgentDoesNotCreateInstructionsFile()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = CreateDesktopAgentProfile(root, profileRoot, situationEnabled: true);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Configure DevCraft", "Add DevCraft To Desktop Agent", "Exit"]);

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.False(File.Exists(Path.Combine(profile, "desktop-agent-instructions.txt")));
    }

    [Fact]
    public void RunAddDevCraftToDesktopAgentWaitsForEnterAfterShowingInstructions()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = CreateDesktopAgentProfile(root, profileRoot, situationEnabled: true);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Configure DevCraft", "Add DevCraft To Desktop Agent", "Exit"]);

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        (string prompt, int statusCount) = Assert.Single(console.WaitPrompts);
        Assert.Equal("Press Enter to return to the menu", prompt);
        Assert.Equal(DesktopAgentInstructionsBuilder.Build(profile), console.StatusMessages[statusCount - 1]);
    }

    [Fact]
    public void RunConfigureSituationalAwarenessWarnsWhenDesktopInstructionsChange()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = CreateDesktopAgentProfile(root, profileRoot, situationEnabled: false);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new(
            [],
            ["Configure DevCraft", "Configure Situational Awareness", "Yes", "Months & Weeks", "File", "Exit"]);

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        string notice = Assert.Single(console.StatusMessages, IsOutOfDateNotice);
        Assert.Contains("Add DevCraft To Desktop Agent", notice);
        (string prompt, int statusCount) = Assert.Single(console.WaitPrompts);
        Assert.Equal("Press Enter to return to the menu", prompt);
        Assert.Equal(notice, console.StatusMessages[statusCount - 1]);
    }

    [Fact]
    public void RunConfigureSituationalAwarenessDoesNotWarnWhenDesktopInstructionsUnchanged()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = CreateDesktopAgentProfile(root, profileRoot, situationEnabled: true);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new(
            [],
            ["Configure DevCraft", "Configure Situational Awareness", "Yes", "Months & Weeks", "File", "Exit"]);

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.Contains("Situational awareness configuration updated.", console.StatusMessages);
        Assert.DoesNotContain(console.StatusMessages, IsOutOfDateNotice);
        Assert.Empty(console.WaitPrompts);
    }

    [Fact]
    public void RunConfigureSituationalAwarenessWarnsWhenDisabled()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = CreateDesktopAgentProfile(root, profileRoot, situationEnabled: true);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Configure DevCraft", "Configure Situational Awareness", "No", "Exit"]);

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.Single(console.StatusMessages, IsOutOfDateNotice);
        Assert.Single(console.WaitPrompts);
    }

    [Fact]
    public void RunConfigureSituationalAwarenessNoticeDoesNotExposeConnectionString()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = CreateDesktopAgentProfile(root, profileRoot, situationEnabled: false);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        const string connectionString = "mongodb://user:secret@localhost:27017/DevCraft";
        FakeConsoleInteraction console = new(
            [],
            ["Configure DevCraft", "Configure Situational Awareness", "Yes", "Months & Weeks", "Database", "Exit"]);
        console.EnqueueValueEdit(connectionString);

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.Single(console.StatusMessages, IsOutOfDateNotice);
        Assert.DoesNotContain(console.StatusMessages, message => message.Contains(connectionString, StringComparison.Ordinal));
        Assert.DoesNotContain(console.StatusMessages, message => message.Contains("secret", StringComparison.Ordinal));
    }

    [Fact]
    public void ConfigureSituationalAwarenessPrefillsExistingConnectionString()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        const string existing = "mongodb://localhost:27017/Existing";
        DevCraftProfileConfigurationWriter.Write(
            profile,
            new DevCraftProfileConfiguration([], [], [], [], [], [], [], true, "weeks", "database", existing));
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new(
            [],
            ["Configure DevCraft", "Configure Situational Awareness", "Yes", "Months & Weeks", "Database", "Exit"]);

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.Equal([existing], console.ValueEditInitialValues);
        Assert.Empty(console.AskPrompts);
        Assert.Equal(existing, ProfileConfigurationReader.Read(profile).SituationConnection);
    }

    [Fact]
    public void ConfigureSituationalAwarenessCancelWithoutConnectionStringDoesNotChangeConfiguration()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        DevCraftProfileConfigurationWriter.Write(profile, new DevCraftProfileConfiguration([], [], [], [], [], [], [], true));
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new(
            [],
            ["Configure DevCraft", "Configure Situational Awareness", "Yes", "Months & Weeks", "Database", "Exit"]);
        console.EnqueueValueEdit(string.Empty, saved: false);

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        DevCraftProfileConfiguration configuration = ProfileConfigurationReader.Read(profile);
        Assert.Equal("file", configuration.SituationStorage);
        Assert.Null(configuration.SituationConnection);
        Assert.Contains("A database connection string is required. Situational awareness was not changed.", console.StatusMessages);
    }

    [Fact]
    public void ConfigureDevCraftListsEditSoulFirst()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        (StartupContext context, _) = CreateSituationContext(root, profileRoot);
        FakeConsoleInteraction console = new([], ["Configure DevCraft", "Back", "Exit"]);

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        int index = console.SelectTitles.IndexOf("Configure DevCraft");
        Assert.Equal("Edit Soul", console.SelectChoices[index][0]);
    }

    [Fact]
    public void EditSoulPrefillsExistingAnswers()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        (StartupContext context, _) = CreateSituationContext(root, profileRoot);
        SoulFileWriter.Write(context.SoulFilePath, SampleSoul);
        FakeConsoleInteraction console = new([], ["Configure DevCraft", "Edit Soul", "Keep current: codex", "Exit"]);
        console.EnqueueValueEdit("John");
        console.EnqueueTextEdit("I build and design software.");

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.Equal(["John", "Jarvis"], console.ValueEditInitialValues);
        Assert.Equal(["I build software.", "Be concise."], console.TextEditInitialTexts);
        Assert.Empty(console.AskPrompts);
        Assert.Equal("I build and design software.", SoulAnswersStore.Read(context.SoulFilePath).WorkAndAssistanceContext);
        Assert.Contains("Soul updated.", console.StatusMessages);
    }

    [Fact]
    public void EditSoulSavingUnchangedLeavesFileContentEqual()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        (StartupContext context, _) = CreateSituationContext(root, profileRoot);
        SoulFileWriter.Write(context.SoulFilePath, SampleSoul);
        string before = File.ReadAllText(context.SoulFilePath);
        FakeConsoleInteraction console = new([], ["Configure DevCraft", "Edit Soul", "Keep current: codex", "Exit"]);

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.Equal(before, File.ReadAllText(context.SoulFilePath));
    }

    [Fact]
    public void EditSoulCancelDoesNotWrite()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        (StartupContext context, _) = CreateSituationContext(root, profileRoot);
        SoulFileWriter.Write(context.SoulFilePath, SampleSoul);
        string before = File.ReadAllText(context.SoulFilePath);
        FakeConsoleInteraction console = new([], ["Configure DevCraft", "Edit Soul", "Exit"]);
        console.EnqueueValueEdit("Changed", saved: false);

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.Equal(before, File.ReadAllText(context.SoulFilePath));
        Assert.Contains("Soul was not changed.", console.StatusMessages);
    }

    [Fact]
    public void EditSoulMissingFileReportsAndDoesNotCreate()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        (StartupContext context, _) = CreateSituationContext(root, profileRoot);
        FakeConsoleInteraction console = new([], ["Configure DevCraft", "Edit Soul", "Exit"]);

        DevCraftMenuCommand.Run(context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.False(File.Exists(context.SoulFilePath));
        Assert.Contains(console.StatusMessages, message => message.StartsWith("No soul.md found", StringComparison.Ordinal));
        Assert.Empty(console.ValueEditTitles);
    }

    private static readonly SoulSetupAnswers SampleSoul = new("John", "I build software.", "Jarvis", "Be concise.", "codex");

    private static bool IsOutOfDateNotice(string message)
    {
        return message.Contains("out of date", StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateDesktopAgentProfile(TestDirectory root, TestDirectory profileRoot, bool situationEnabled)
    {
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        DevCraftProfileConfigurationWriter.Write(profile, new DevCraftProfileConfiguration([], [], [], [], [], [], [], situationEnabled));
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);

        return profile;
    }
}
