using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class FeatureCommandTests
{
    private static readonly ProjectProfile SampleProfile = new(
        "Sample Project",
        "Validate feature command behavior.",
        "A sample project for feature command tests.");

    [Fact]
    public void NewCreatesFeatureInRepositoryConfigurationAndLaunchesAi()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        DevCraftProfileConfigurationWriter.Write(
            profile,
            new DevCraftProfileConfiguration([], [], [], [], [], [], [])
            {
                DefaultFeatureStorage = FeatureStorageMode.RepoCentral,
            });
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, soul);
        FakeConsoleInteraction console = new([]);
        FakeFeatureAiSessionLauncher launcher = new();

        FeatureCommand.Run(context, ["new", "First", "Feature"], console, launcher);

        ProjectDevCraftConfiguration configuration = ProjectDevCraftConfigurationReader.Read(root.Path)!;
        DevCraftFeature feature = Assert.Single(configuration.Features);
        Assert.Equal("First Feature", feature.Name);
        Assert.Equal("first-feature", feature.Slug);
        Assert.Equal("repo-central", feature.StorageType);
        Assert.Null(configuration.SelectedFeatureStorage);
        Assert.DoesNotContain(console.StatusMessages, message => message.StartsWith("Feature storage set", StringComparison.OrdinalIgnoreCase));
        Assert.True(Directory.Exists(Path.Combine(root.Path, ".devcraft", "features", feature.FolderName)));
        Assert.Single(launcher.Launches);
        Assert.Equal("repo-central", launcher.ProjectConfigurations[0].SelectedFeatureStorage);
        Assert.Equal("Create a new feature.", launcher.Launches[0].Instruction);
    }

    [Fact]
    public void ListUsesProfileDefaultWhenRepositoryHasNoOverride()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        DevCraftProfileConfigurationWriter.Write(
            profile,
            new DevCraftProfileConfiguration([], [], [], [], [], [], [])
            {
                DefaultFeatureStorage = FeatureStorageMode.SystemCentral,
            });
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        DevCraftFeature feature = new(
            "Pending Storage Feature",
            "pending-storage-feature",
            "Feature in repository.",
            "feature-folder",
            "repo-central",
            null);
        ProjectDevCraftConfigurationStore.Write(
            root.Path,
            new ProjectDevCraftConfiguration(
                Guid.NewGuid().ToString(),
                null,
                "features.json",
                SampleProfile,
                [feature]));
        StartupContext context = new(root.Path, profile, soul);
        FakeConsoleInteraction console = new([], ["Pending Storage Feature (pending-storage-feature)", StartConversation]);
        FakeFeatureAiSessionLauncher launcher = new();

        FeatureCommand.Run(context, ["list"], console, launcher);

        ProjectDevCraftConfiguration configuration = ProjectDevCraftConfigurationReader.Read(root.Path)!;
        Assert.Null(configuration.SelectedFeatureStorage);
        Assert.DoesNotContain(console.StatusMessages, message => message.StartsWith("Feature storage set", StringComparison.OrdinalIgnoreCase));
        Assert.Single(launcher.Launches);
        Assert.Equal("system-central", launcher.ProjectConfigurations[0].SelectedFeatureStorage);
    }

    [Fact]
    public void ListIncludesBackAndDoesNotSelectClientWhenBackIsSelected()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        DevCraftFeature feature = new(
            "Deferred Client Feature",
            "deferred-client-feature",
            "Feature in repository.",
            "feature-folder",
            "repo-central",
            null);
        ProjectDevCraftConfigurationStore.Write(
            root.Path,
            new ProjectDevCraftConfiguration(
                Guid.NewGuid().ToString(),
                null,
                "features.json",
                SampleProfile,
                [feature]));
        StartupContext context = new(root.Path, profile, soul);
        FakeConsoleInteraction console = new([], ["Back"]);
        FakeFeatureAiSessionLauncher launcher = new();
        bool clientSelected = false;

        FeatureCommand.Run(
            context,
            ["list"],
            console,
            launcher,
            selectHandoff: () =>
            {
                clientSelected = true;
                return new AgentHandoff(SupportedTerminalClientCatalog.Create()[0], string.Empty);
            });

        int featureListIndex = console.SelectTitles.FindIndex(title => title == "Select a feature");
        Assert.True(featureListIndex >= 0);
        Assert.Contains("Back", console.SelectChoices[featureListIndex]);
        Assert.False(clientSelected);
        Assert.Empty(launcher.Launches);
    }

    [Fact]
    public void ListSelectsClientAfterFeatureSelection()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        DevCraftFeature feature = new(
            "Deferred Client Feature",
            "deferred-client-feature",
            "Feature in repository.",
            "feature-folder",
            "repo-central",
            null);
        ProjectDevCraftConfigurationStore.Write(
            root.Path,
            new ProjectDevCraftConfiguration(
                Guid.NewGuid().ToString(),
                null,
                "features.json",
                SampleProfile,
                [feature]));
        StartupContext context = new(root.Path, profile, soul);
        FakeConsoleInteraction console = new([], ["Deferred Client Feature (deferred-client-feature)", StartConversation]);
        FakeFeatureAiSessionLauncher launcher = new();
        SupportedTerminalClient selectedClient = SupportedTerminalClientCatalog.Create()[1];
        bool featureSelectedBeforeClient = false;

        FeatureCommand.Run(
            context,
            ["list"],
            console,
            launcher,
            selectHandoff: () =>
            {
                featureSelectedBeforeClient = console.StatusMessages.Contains("Selected feature: Deferred Client Feature");
                return new AgentHandoff(selectedClient, string.Empty);
            });

        Assert.True(featureSelectedBeforeClient);
        Assert.Single(launcher.Launches);
        Assert.Equal(selectedClient, launcher.Launches[0].Client);
    }

    [Fact]
    public void NewUsesRepositoryOverrideInsteadOfProfileDefault()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        DevCraftProfileConfigurationWriter.Write(
            profile,
            new DevCraftProfileConfiguration([], [], [], [], [], [], [])
            {
                DefaultFeatureStorage = FeatureStorageMode.SystemCentral,
            });
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProjectDevCraftConfigurationStore.Write(
            root.Path,
            new ProjectDevCraftConfiguration(
                Guid.NewGuid().ToString(),
                FeatureStorageMode.RepoCentral,
                "features.json",
                SampleProfile,
                []));
        StartupContext context = new(root.Path, profile, soul);
        FakeConsoleInteraction console = new([]);
        FakeFeatureAiSessionLauncher launcher = new();

        FeatureCommand.Run(context, ["new", "Repository", "Override"], console, launcher);

        ProjectDevCraftConfiguration configuration = ProjectDevCraftConfigurationReader.Read(root.Path)!;
        DevCraftFeature feature = Assert.Single(configuration.Features);
        Assert.Equal(FeatureStorageMode.RepoCentral, feature.StorageType);
        Assert.Equal(FeatureStorageMode.RepoCentral, configuration.SelectedFeatureStorage);
        Assert.True(Directory.Exists(Path.Combine(root.Path, ".devcraft", "features", feature.FolderName)));
        Assert.Equal(FeatureStorageMode.RepoCentral, launcher.ProjectConfigurations[0].SelectedFeatureStorage);
    }

    [Fact]
    public void ListUsesRepositoryConfigurationForAnyStorageMode()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        DevCraftFeature feature = new(
            "GitHub Backed Feature",
            "github-backed-feature",
            "Feature stored in GitHub.",
            "GH-123",
            "github-issue",
            "https://example.test/issues/123");
        ProjectDevCraftConfigurationStore.Write(
            root.Path,
            new ProjectDevCraftConfiguration(
                Guid.NewGuid().ToString(),
                "github-issue",
                "features.json",
                SampleProfile,
                [feature]));
        StartupContext context = new(root.Path, profile, soul);
        FakeConsoleInteraction console = new([], ["GitHub Backed Feature (github-backed-feature)", StartConversation]);
        FakeFeatureAiSessionLauncher launcher = new();

        FeatureCommand.Run(context, ["list"], console, launcher);

        Assert.DoesNotContain(console.StatusMessages, message => message.Contains("not available", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(console.StatusMessages, message => message == "Selected feature: GitHub Backed Feature");
        Assert.Single(launcher.Launches);
        Assert.Equal("Work on existing feature.", launcher.Launches[0].Instruction);
    }

    [Fact]
    public void NewSyncsSystemCentralProjectIndex()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(profile, "features"));
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        string projectKey = Guid.NewGuid().ToString();
        ProjectDevCraftConfigurationStore.Write(
            root.Path,
            new ProjectDevCraftConfiguration(
                projectKey,
                "system-central",
                "features.json",
                SampleProfile,
                []));
        StartupContext context = new(root.Path, profile, soul);
        FakeConsoleInteraction console = new([]);
        FakeFeatureAiSessionLauncher launcher = new();

        FeatureCommand.Run(context, ["new", "Central", "Feature"], console, launcher);

        Assert.Empty(ProjectDevCraftConfigurationReader.Read(root.Path)!.Features);
        SystemCentralProjectsIndex index = SystemCentralProjectsStore.Read(profile);
        SystemCentralProject project = Assert.Single(index.Projects);
        Assert.Equal(projectKey, project.ProjectKey);
        Assert.Equal(root.Path, Assert.Single(project.WorkingCopies).Path);
        Assert.Equal(Path.GetFileName(root.Path), project.RepositoryName);
        DevCraftFeature trackedFeature = Assert.Single(project.Features);
        Assert.Equal("Central Feature", trackedFeature.Name);
        Assert.Equal("Discovery", trackedFeature.Status);
        string projectsJson = File.ReadAllText(Path.Combine(profile, "features", "projects.json"));
        Assert.Contains("\"id\":", projectsJson);
        Assert.Contains("\"working-copies\":", projectsJson);
        Assert.Contains("\"repo-name\":", projectsJson);
        Assert.Contains("\"feature-name\": \"Central Feature\"", projectsJson);
        Assert.Contains("\"work-item-id\": null", projectsJson);
        Assert.Contains("\"status\": \"Discovery\"", projectsJson);
        Assert.True(Directory.Exists(Path.Combine(profile, "features", trackedFeature.FolderName)));
    }

    [Fact]
    public void NewRecordsExistingRepositoryFeaturesInProjectIndex()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(Path.Combine(profile, "features"));
        string projectKey = Guid.NewGuid().ToString();
        WriteSystemCentralRepository(
            root.Path,
            projectKey,
            [
                SampleFeature("First Existing", "first-existing", "folder-one", "Implementation"),
                SampleFeature("Second Existing", "second-existing", "folder-two", "Complete"),
            ]);
        StartupContext context = new(root.Path, profile, soul);

        FeatureCommand.Run(context, ["new", "Third", "Feature"], new FakeConsoleInteraction([]), new FakeFeatureAiSessionLauncher());

        SystemCentralProject project = Assert.Single(SystemCentralProjectsStore.Read(profile).Projects);
        Assert.Equal(projectKey, project.ProjectKey);
        Assert.Equal(
            ["first-existing", "second-existing", "third-feature"],
            project.Features.Select(feature => feature.Slug));
        Assert.Equal("Complete", project.Features[1].Status);
    }

    [Fact]
    public void ListHealsProjectIndexMissingRepositoryFeatures()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(Path.Combine(profile, "features"));
        string projectKey = Guid.NewGuid().ToString();
        WriteSystemCentralRepository(
            root.Path,
            projectKey,
            [
                SampleFeature("First Existing", "first-existing", "folder-one", "Implementation"),
                SampleFeature("Second Existing", "second-existing", "folder-two", "Complete"),
            ]);
        SystemCentralProjectsStore.Write(
            profile,
            new SystemCentralProjectsIndex(
            [
                CentralProject(
                    projectKey,
                    "Sample Project",
                    "sample-project",
                    root.Path,
                    [SampleFeature("Second Existing", "second-existing", "folder-two", "Discovery")]),
            ]));
        StartupContext context = new(root.Path, profile, soul);

        FeatureCommand.Run(context, ["list"], new FakeConsoleInteraction([], ["Back"]), new FakeFeatureAiSessionLauncher());

        SystemCentralProject project = Assert.Single(SystemCentralProjectsStore.Read(profile).Projects);
        Assert.Equal(2, project.Features.Count);
        Assert.Contains(project.Features, feature => feature.Slug == "first-existing" && feature.Status == "Implementation");
        Assert.Contains(project.Features, feature => feature.Slug == "second-existing" && feature.Status == "Discovery");
        Assert.Empty(ProjectDevCraftConfigurationReader.Read(root.Path)!.Features);
    }

    [Fact]
    public void NewRecordsRepoCentralFeatureInProjectIndex()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        string projectKey = Guid.NewGuid().ToString();
        ProjectDevCraftConfigurationStore.Write(
            root.Path,
            new ProjectDevCraftConfiguration(
                projectKey,
                FeatureStorageMode.RepoCentral,
                "features.json",
                SampleProfile,
                []));
        StartupContext context = new(root.Path, profile, soul);

        FeatureCommand.Run(context, ["new", "Local", "Feature"], new FakeConsoleInteraction([]), new FakeFeatureAiSessionLauncher());

        SystemCentralProject project = Assert.Single(SystemCentralProjectsStore.Read(profile).Projects);
        DevCraftFeature feature = Assert.Single(project.Features);
        Assert.Equal(projectKey, project.ProjectKey);
        Assert.Equal("local-feature", feature.Slug);
        Assert.Equal(FeatureStorageMode.RepoCentral, feature.StorageType);
        Assert.False(Directory.Exists(Path.Combine(profile, "features", feature.FolderName)));
    }

    [Fact]
    public void ListPreservesCentralOnlyFeaturesAndOtherProjects_CentralRecordWins()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(Path.Combine(profile, "features"));
        string projectKey = Guid.NewGuid().ToString();
        WriteSystemCentralRepository(
            root.Path,
            projectKey,
            [SampleFeature("Shared", "shared", "folder-shared", "Implementation")]);
        SystemCentralProject otherProject = CentralProject(
            Guid.NewGuid().ToString(),
            "Other Project",
            "other-project",
            "/tmp/other-project",
            [SampleFeature("Other Feature", "other-feature", "folder-other", "Complete")]);
        SystemCentralProjectsStore.Write(
            profile,
            new SystemCentralProjectsIndex(
            [
                otherProject,
                CentralProject(
                    projectKey,
                    "Sample Project",
                    "sample-project",
                    "/tmp/old-location",
                    [
                        SampleFeature("Central Only", "central-only", "folder-central", "Complete"),
                        SampleFeature("Shared", "shared-old-slug", "folder-shared", "Discovery"),
                    ]),
            ]));
        StartupContext context = new(root.Path, profile, soul);

        FeatureCommand.Run(context, ["list"], new FakeConsoleInteraction([], ["Back"]), new FakeFeatureAiSessionLauncher());

        IReadOnlyList<SystemCentralProject> projects = SystemCentralProjectsStore.Read(profile).Projects;
        Assert.Equal(2, projects.Count);
        Assert.Equal(otherProject.ProjectKey, projects[0].ProjectKey);
        Assert.Equal("other-feature", Assert.Single(projects[0].Features).Slug);
        Assert.Contains(projects[1].WorkingCopies, copy => copy.Path == root.Path);
        Assert.Equal(["central-only", "shared-old-slug"], projects[1].Features.Select(feature => feature.Slug));
        Assert.Equal("Discovery", projects[1].Features[1].Status);
    }

    [Fact]
    public void ListCarriesCentralStatusIntoRepoCentralConfiguration()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string projectKey = Guid.NewGuid().ToString();
        WriteSystemCentralRepository(
            root.Path,
            projectKey,
            [LocalFeature("Shipped", "shipped", "folder-shipped", "Implementation")]);
        SystemCentralProjectsStore.Write(
            profile,
            new SystemCentralProjectsIndex(
            [
                CentralProject(
                    projectKey,
                    "Sample Project",
                    "sample-project",
                    root.Path,
                    [LocalFeature("Shipped", "shipped", "folder-shipped", "Complete")]),
            ]));
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));

        FeatureCommand.Run(context, ["list"], new FakeConsoleInteraction([], ["Back"]), new FakeFeatureAiSessionLauncher());

        Assert.Equal("Complete", Assert.Single(ProjectDevCraftConfigurationReader.Read(root.Path)!.Features).Status);
        Assert.Equal("Complete", Assert.Single(Assert.Single(SystemCentralProjectsStore.Read(profile).Projects).Features).Status);
    }

    [Fact]
    public void ListStartsWithBackAndCompletedAndShowsOnlyInProgressFeatures()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        WriteSystemCentralRepository(
            root.Path,
            Guid.NewGuid().ToString(),
            [
                SampleFeature("Done Feature", "done-feature", "folder-done", "Complete"),
                SampleFeature("Active Feature", "active-feature", "folder-active", "Implementation"),
                SampleFeature("New Idea", "new-idea", "folder-idea", "Discovery"),
            ]);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Back"]);

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher());

        int listIndex = console.SelectTitles.FindIndex(title => title == "Select a feature");
        Assert.Equal(
            ["Back", "Completed", "Active Feature (active-feature)", "New Idea (new-idea)"],
            console.SelectChoices[listIndex]);
    }

    [Fact]
    public void ListCompletedShowsOnlyCompletedFeaturesAndBackReturnsToInProgress()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        WriteSystemCentralRepository(
            root.Path,
            Guid.NewGuid().ToString(),
            [
                SampleFeature("Done Feature", "done-feature", "folder-done", "Complete"),
                SampleFeature("Active Feature", "active-feature", "folder-active", "Implementation"),
            ]);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Completed", "Back", "Back"]);
        FakeFeatureAiSessionLauncher launcher = new();

        FeatureCommand.Run(context, ["list"], console, launcher);

        Assert.Equal(["Select a feature", "Completed features", "Select a feature"], console.SelectTitles);
        Assert.Equal(["Back", "Done Feature (done-feature)"], console.SelectChoices[1]);
        Assert.Empty(launcher.Launches);
    }

    [Fact]
    public void ListCompletedFeatureCanBeSelected()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        WriteSystemCentralRepository(
            root.Path,
            Guid.NewGuid().ToString(),
            [SampleFeature("Done Feature", "done-feature", "folder-done", "Complete")]);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Completed", "Done Feature (done-feature)", "Back"]);

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher());

        Assert.Equal(["Back", "Completed"], console.SelectChoices[0]);
        Assert.Contains("Selected feature: Done Feature", console.StatusMessages);
        Assert.Contains("Feature actions", console.SelectTitles);
    }

    [Fact]
    public void ListShowsFeatureActionsAfterSelection()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        WriteSystemCentralRepository(
            root.Path,
            Guid.NewGuid().ToString(),
            [SampleFeature("First Existing", "first-existing", "folder-one", "Implementation")]);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["First Existing (first-existing)", StartConversation]);
        FakeFeatureAiSessionLauncher launcher = new();

        FeatureCommand.Run(context, ["list"], console, launcher);

        int actionsIndex = console.SelectTitles.FindIndex(title => title == "Feature actions");
        Assert.True(actionsIndex > console.SelectTitles.FindIndex(title => title == "Select a feature"));
        Assert.Equal([EditFeatureName, "Edit Work Item", "Set Feature to Current Branch", OpenFeatureDirectory, StartConversation, "Commit Code", "Switch and Pull", "Create Branch", "Merge From", "Back"], console.SelectChoices[actionsIndex]);
        Assert.Equal("Work on existing feature.", Assert.Single(launcher.Launches).Instruction);
    }

    [Fact]
    public void ListCreateBranchRunsGitAndReturnsToFeatureActionsWithNotice()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        WriteSystemCentralRepository(
            root.Path,
            Guid.NewGuid().ToString(),
            [SampleFeature("First Existing", "first-existing", "folder-one", "Implementation")]);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["First Existing (first-existing)", "Create Branch", "Enter a custom branch name", "Back"]);
        console.EnqueueValueEdit("topic/new-work");
        FakeGitRunner git = new FakeGitRunner()
            .On("symbolic-ref --short -q HEAD", output: "main\n")
            .On("show-ref --verify --quiet refs/heads/topic/new-work", exitCode: 1);
        FakeFeatureAiSessionLauncher launcher = new();

        FeatureCommand.Run(context, ["list"], console, launcher, gitRunner: git, commitAgent: new FakeRepositoryDocumentAgent());

        Assert.Contains("switch -c topic/new-work", git.Calls);
        Assert.Contains(
            $"Created branch topic/new-work from main and switched to it.{Environment.NewLine}{Environment.NewLine}Feature actions",
            console.SelectTitles);
        Assert.Empty(launcher.Launches);
    }

    [Fact]
    public void ListFeatureActionsBackDoesNotLaunchOrSelectClient()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        WriteSystemCentralRepository(
            root.Path,
            Guid.NewGuid().ToString(),
            [SampleFeature("First Existing", "first-existing", "folder-one", "Implementation")]);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["First Existing (first-existing)", "Back"]);
        FakeFeatureAiSessionLauncher launcher = new();
        bool clientSelected = false;

        FeatureCommand.Run(
            context,
            ["list"],
            console,
            launcher,
            selectHandoff: () =>
            {
                clientSelected = true;
                return new AgentHandoff(SupportedTerminalClientCatalog.Create()[0], string.Empty);
            });

        Assert.Contains("Feature actions", console.SelectTitles);
        Assert.False(clientSelected);
        Assert.Empty(launcher.Launches);
    }

    [Fact]
    public void ListEditFeatureNameRenamesFeatureInBothIndexes()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string projectKey = Guid.NewGuid().ToString();
        WriteSystemCentralRepository(
            root.Path,
            projectKey,
            [
                LocalFeature("First Existing", "first-existing", "folder-one", "Implementation"),
                LocalFeature("Second Existing", "second-existing", "folder-two", "Complete"),
            ]);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["First Existing (first-existing)", EditFeatureName, "Back"]);
        console.EnqueueValueEdit("  Renamed Feature  ");
        FakeFeatureAiSessionLauncher launcher = new();

        FeatureCommand.Run(context, ["list"], console, launcher);

        DevCraftFeature expected = LocalFeature("First Existing", "first-existing", "folder-one", "Implementation") with
        {
            Name = "Renamed Feature",
        };
        ProjectDevCraftConfiguration configuration = ProjectDevCraftConfigurationReader.Read(root.Path)!;
        Assert.Equal(2, configuration.Features.Count);
        Assert.Equal(expected, configuration.Features[0]);
        Assert.Equal("Second Existing", configuration.Features[1].Name);
        SystemCentralProject project = Assert.Single(SystemCentralProjectsStore.Read(profile).Projects);
        Assert.Equal(projectKey, project.ProjectKey);
        Assert.Equal(2, project.Features.Count);
        Assert.Equal(expected, project.Features[0]);
        Assert.Contains("Feature renamed: First Existing -> Renamed Feature", console.StatusMessages);
        Assert.Equal(
            $"Feature renamed: First Existing -> Renamed Feature{Environment.NewLine}{Environment.NewLine}Feature actions",
            console.SelectTitles[^1]);
        Assert.Empty(launcher.Launches);
    }

    [Fact]
    public void ListEditFeatureNamePreservesOtherFeaturesAndProjects()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string projectKey = Guid.NewGuid().ToString();
        WriteSystemCentralRepository(
            root.Path,
            projectKey,
            [SampleFeature("Shared", "shared", "folder-shared", "Implementation")]);
        SystemCentralProject otherProject = CentralProject(
            Guid.NewGuid().ToString(),
            "Other Project",
            "other-project",
            "/tmp/other-project",
            [SampleFeature("Shared", "shared", "folder-other", "Complete")]);
        SystemCentralProjectsStore.Write(
            profile,
            new SystemCentralProjectsIndex(
            [
                otherProject,
                CentralProject(
                    projectKey,
                    "Sample Project",
                    "sample-project",
                    root.Path,
                    [
                        SampleFeature("Central Only", "central-only", "folder-central", "Complete"),
                        SampleFeature("Shared", "shared", "folder-shared", "Implementation"),
                    ]),
            ]));
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["Shared (shared)", EditFeatureName, "Back"]);
        console.EnqueueValueEdit("Renamed");

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher());

        IReadOnlyList<SystemCentralProject> projects = SystemCentralProjectsStore.Read(profile).Projects;
        Assert.Equal(2, projects.Count);
        Assert.Equal("Shared", Assert.Single(projects[0].Features).Name);
        Assert.Equal(["Central Only", "Renamed"], projects[1].Features.Select(feature => feature.Name));
        Assert.Equal(["central-only", "shared"], projects[1].Features.Select(feature => feature.Slug));
    }

    [Fact]
    public void ListEditFeatureNameRejectsBlankName()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        WriteSystemCentralRepository(
            root.Path,
            Guid.NewGuid().ToString(),
            [LocalFeature("First Existing", "first-existing", "folder-one", "Implementation")]);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["First Existing (first-existing)", EditFeatureName, "Back"]);
        console.EnqueueValueEdit("   ");

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher());

        Assert.Contains("Feature name is required.", console.StatusMessages);
        Assert.Equal("First Existing", Assert.Single(ProjectDevCraftConfigurationReader.Read(root.Path)!.Features).Name);
        Assert.Equal("First Existing", Assert.Single(Assert.Single(SystemCentralProjectsStore.Read(profile).Projects).Features).Name);
    }

    [Fact]
    public void ListEditFeatureNamePrefillsCurrentName()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        WriteSystemCentralRepository(
            root.Path,
            Guid.NewGuid().ToString(),
            [SampleFeature("First Existing", "first-existing", "folder-one", "Implementation")]);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["First Existing (first-existing)", EditFeatureName, "Back"]);

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher());

        Assert.Equal(["First Existing"], console.ValueEditInitialValues);
        Assert.Empty(console.AskPrompts);
        Assert.Contains("Feature name is unchanged.", console.StatusMessages);
    }

    [Fact]
    public void ListEditFeatureNameCancelLeavesIndexesUnchanged()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        WriteSystemCentralRepository(
            root.Path,
            Guid.NewGuid().ToString(),
            [LocalFeature("First Existing", "first-existing", "folder-one", "Implementation")]);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["First Existing (first-existing)", EditFeatureName, "Back"]);
        console.EnqueueValueEdit("Renamed", saved: false);
        string configurationPath = ProjectDevCraftConfigurationStore.PathFor(root.Path);
        DateTime writtenAt = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(configurationPath, writtenAt);

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher());

        Assert.Contains("Feature name is unchanged.", console.StatusMessages);
        Assert.Equal(writtenAt, File.GetLastWriteTimeUtc(configurationPath));
    }

    [Fact]
    public void ListEditFeatureNameIgnoresUnchangedName()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        WriteSystemCentralRepository(
            root.Path,
            Guid.NewGuid().ToString(),
            [LocalFeature("First Existing", "first-existing", "folder-one", "Implementation")]);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["First Existing (first-existing)", EditFeatureName, "Back"]);
        console.EnqueueValueEdit("First Existing");
        string configurationPath = ProjectDevCraftConfigurationStore.PathFor(root.Path);
        DateTime writtenAt = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(configurationPath, writtenAt);

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher());

        Assert.Contains("Feature name is unchanged.", console.StatusMessages);
        Assert.Equal(writtenAt, File.GetLastWriteTimeUtc(configurationPath));
    }

    [Fact]
    public void ListConversationAfterRenameUsesNewName()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        WriteSystemCentralRepository(
            root.Path,
            Guid.NewGuid().ToString(),
            [LocalFeature("First Existing", "first-existing", "folder-one", "Implementation")]);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["First Existing (first-existing)", EditFeatureName, StartConversation]);
        console.EnqueueValueEdit("Renamed Feature");
        FakeFeatureAiSessionLauncher launcher = new();

        FeatureCommand.Run(context, ["list"], console, launcher);

        Assert.Equal("Renamed Feature", Assert.Single(launcher.Launches).Feature.Name);
        Assert.Equal(
            "Renamed Feature",
            Assert.Single(launcher.ProjectConfigurations).Features.Single().Name);
    }

    [Fact]
    public void ListOpenFeatureDirectoryOpensSystemCentralFolder()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string featureDirectory = Path.Combine(profile, "features", "folder-one");
        Directory.CreateDirectory(featureDirectory);
        WriteSystemCentralRepository(
            root.Path,
            Guid.NewGuid().ToString(),
            [SampleFeature("First Existing", "first-existing", "folder-one", "Implementation")]);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["First Existing (first-existing)", OpenFeatureDirectory, "Back"]);
        FakeFeatureAiSessionLauncher launcher = new();
        FakeFeatureDirectoryOpener opener = new();

        FeatureCommand.Run(context, ["list"], console, launcher, directoryOpener: opener);

        Assert.Equal([featureDirectory], opener.OpenedDirectories);
        Assert.Contains($"Opened feature directory in Visual Studio Code: {featureDirectory}", console.StatusMessages);
        Assert.Equal(
            ["Feature actions", $"Opened feature directory in Visual Studio Code: {featureDirectory}{Environment.NewLine}{Environment.NewLine}Feature actions"],
            console.SelectTitles.Skip(1));
        Assert.Empty(launcher.Launches);
    }

    [Fact]
    public void ListOpenFeatureDirectoryOpensRepoCentralFolder()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string featureDirectory = Path.Combine(root.Path, ".devcraft", "features", "folder-one");
        Directory.CreateDirectory(featureDirectory);
        WriteSystemCentralRepository(
            root.Path,
            Guid.NewGuid().ToString(),
            [
                SampleFeature("First Existing", "first-existing", "folder-one", "Implementation") with
                {
                    StorageType = FeatureStorageMode.RepoCentral,
                },
            ]);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["First Existing (first-existing)", OpenFeatureDirectory, "Back"]);
        FakeFeatureDirectoryOpener opener = new();

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher(), directoryOpener: opener);

        Assert.Equal([featureDirectory], opener.OpenedDirectories);
    }

    [Fact]
    public void ListOpenFeatureDirectoryReportsExternalStorage()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        WriteSystemCentralRepository(
            root.Path,
            Guid.NewGuid().ToString(),
            [
                SampleFeature("First Existing", "first-existing", "GH-123", "Implementation") with
                {
                    StorageType = "github-issue",
                },
            ]);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["First Existing (first-existing)", OpenFeatureDirectory, "Back"]);
        FakeFeatureDirectoryOpener opener = new();

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher(), directoryOpener: opener);

        Assert.Empty(opener.OpenedDirectories);
        Assert.Contains("First Existing uses github-issue storage and has no local feature directory.", console.StatusMessages);
    }

    [Fact]
    public void ListOpenFeatureDirectoryReportsMissingDirectory()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        WriteSystemCentralRepository(
            root.Path,
            Guid.NewGuid().ToString(),
            [SampleFeature("First Existing", "first-existing", "folder-one", "Implementation")]);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["First Existing (first-existing)", OpenFeatureDirectory, "Back"]);
        FakeFeatureDirectoryOpener opener = new();

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher(), directoryOpener: opener);

        Assert.Empty(opener.OpenedDirectories);
        Assert.Contains(
            $"Feature directory not found: {Path.Combine(profile, "features", "folder-one")}",
            console.StatusMessages);
    }

    [Fact]
    public void ListOpenFeatureDirectoryReportsWhenCodeCannotStart()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(Path.Combine(profile, "features", "folder-one"));
        WriteSystemCentralRepository(
            root.Path,
            Guid.NewGuid().ToString(),
            [SampleFeature("First Existing", "first-existing", "folder-one", "Implementation")]);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["First Existing (first-existing)", OpenFeatureDirectory, "Back"]);
        FakeFeatureDirectoryOpener opener = new()
        {
            Result = false,
        };

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher(), directoryOpener: opener);

        Assert.Single(opener.OpenedDirectories);
        Assert.Contains(
            "Could not start Visual Studio Code. Make sure the `code` command is installed and on your PATH.",
            console.StatusMessages);
    }

    [Fact]
    public void NewAsksStartingPromptAndPassesItToLauncher()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = RepoCentralContext(root, profileRoot);
        FakeConsoleInteraction console = new([]);
        console.EnqueueTextEdit("Start with the acceptance criteria.");
        FakeFeatureAiSessionLauncher launcher = new();

        FeatureCommand.Run(context, ["new", "Prompted", "Feature"], console, launcher);

        Assert.Equal(["Starting Prompt"], console.TextEditTitles);
        Assert.Equal([string.Empty], console.TextEditInitialTexts);
        Assert.Single(launcher.Launches);
        Assert.Equal("Start with the acceptance criteria.", Assert.Single(launcher.StartingPrompts));
    }

    [Fact]
    public void NewCancelledStartingPromptCreatesNothingAndLaunchesNothing()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = RepoCentralContext(root, profileRoot);
        FakeConsoleInteraction console = new([]);
        console.EnqueueTextEdit("Discarded", saved: false);
        FakeFeatureAiSessionLauncher launcher = new();

        FeatureCommand.Run(context, ["new", "Cancelled", "Feature"], console, launcher);

        Assert.Empty(ProjectDevCraftConfigurationReader.Read(root.Path)!.Features);
        Assert.Empty(launcher.Launches);
        Assert.Contains("Agent handoff cancelled.", console.StatusMessages);
    }

    [Fact]
    public void ListStartConversationPassesStartingPrompt()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = RepoCentralContext(root, profileRoot, ExistingFeature());
        FakeConsoleInteraction console = new([], ["Existing Feature (existing-feature)", StartConversation]);
        console.EnqueueTextEdit("Line one\nLine two");
        FakeFeatureAiSessionLauncher launcher = new();

        FeatureCommand.Run(context, ["list"], console, launcher);

        Assert.Single(launcher.Launches);
        Assert.Equal("Line one\nLine two", Assert.Single(launcher.StartingPrompts));
    }

    [Fact]
    public void ListStartConversationCancelledStartingPromptDoesNotLaunch()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        StartupContext context = RepoCentralContext(root, profileRoot, ExistingFeature());
        FakeConsoleInteraction console = new([], ["Existing Feature (existing-feature)", StartConversation]);
        console.EnqueueTextEdit("Discarded", saved: false);
        FakeFeatureAiSessionLauncher launcher = new();

        FeatureCommand.Run(context, ["list"], console, launcher);

        Assert.Empty(launcher.Launches);
        Assert.Contains("Agent handoff cancelled.", console.StatusMessages);
    }

    private static DevCraftFeature ExistingFeature()
    {
        return new DevCraftFeature(
            "Existing Feature",
            "existing-feature",
            "Feature in repository.",
            "existing-feature-folder",
            FeatureStorageMode.RepoCentral,
            null);
    }

    private static StartupContext RepoCentralContext(TestDirectory root, TestDirectory profileRoot, params DevCraftFeature[] features)
    {
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProjectDevCraftConfigurationStore.Write(
            root.Path,
            new ProjectDevCraftConfiguration(
                Guid.NewGuid().ToString(),
                FeatureStorageMode.RepoCentral,
                "features.json",
                SampleProfile,
                features));

        return new StartupContext(root.Path, profile, Path.Combine(profile, "soul.md"));
    }

    [Fact]
    public void List_SystemCentral_ShowsFeatureCreatedInAnotherWorkingCopy()
    {
        using TestDirectory root = new();
        string profile = Path.Combine(root.Path, ".DevCraft");
        string first = Path.Combine(root.Path, "Active Work 1", "ProjectCodeArc");
        string second = Path.Combine(root.Path, "Active Work 2", "ProjectCodeArc");
        string projectKey = Guid.NewGuid().ToString();
        WriteSystemCentralRepository(first, projectKey, []);
        WriteSystemCentralRepository(second, projectKey, []);

        FeatureCommand.Run(new StartupContext(first, profile, Path.Combine(profile, "soul.md")), ["new", "Shared", "Work"], new FakeConsoleInteraction([]), new FakeFeatureAiSessionLauncher());
        FakeConsoleInteraction console = new([], ["Back"]);
        FeatureCommand.Run(new StartupContext(second, profile, Path.Combine(profile, "soul.md")), ["list"], console, new FakeFeatureAiSessionLauncher());

        Assert.Contains("Shared Work (shared-work)", console.SelectChoices[console.SelectTitles.IndexOf("Select a feature")]);
    }

    [Fact]
    public void New_SystemCentral_WritesOnlyProjectsJson()
    {
        using TestDirectory root = new();
        string profile = Path.Combine(root.Path, ".DevCraft");
        string repo = Path.Combine(root.Path, "repo");
        WriteSystemCentralRepository(repo, Guid.NewGuid().ToString(), []);

        FeatureCommand.Run(new StartupContext(repo, profile, Path.Combine(profile, "soul.md")), ["new", "Central", "Only"], new FakeConsoleInteraction([]), new FakeFeatureAiSessionLauncher());

        Assert.Empty(ProjectDevCraftConfigurationReader.Read(repo)!.Features);
        Assert.Equal("central-only", Assert.Single(Assert.Single(SystemCentralProjectsStore.Read(profile).Projects).Features).Slug);
    }

    [Fact]
    public void Rename_SystemCentral_UpdatesCentralOnly()
    {
        using TestDirectory root = new();
        string profile = Path.Combine(root.Path, ".DevCraft");
        string repo = Path.Combine(root.Path, "repo");
        string projectKey = Guid.NewGuid().ToString();
        WriteSystemCentralRepository(repo, projectKey, []);
        SystemCentralProjectsStore.Write(profile, new SystemCentralProjectsIndex(
            [CentralProject(projectKey, "Sample Project", "sample-project", repo, [SampleFeature("Before", "before", "folder-before", "Discovery")])]));
        FakeConsoleInteraction console = new([], ["Before (before)", EditFeatureName, "Back"]);
        console.EnqueueValueEdit("After");

        FeatureCommand.Run(new StartupContext(repo, profile, Path.Combine(profile, "soul.md")), ["list"], console, new FakeFeatureAiSessionLauncher());

        Assert.Equal("After", Assert.Single(Assert.Single(SystemCentralProjectsStore.Read(profile).Projects).Features).Name);
        Assert.Empty(ProjectDevCraftConfigurationReader.Read(repo)!.Features);
    }

    [Fact]
    public void Rename_RepoCentral_UpdatesBoth()
    {
        using TestDirectory root = new();
        string profile = Path.Combine(root.Path, ".DevCraft");
        string repo = Path.Combine(root.Path, "repo");
        DevCraftFeature local = SampleFeature("Before", "before", "folder-before", "Discovery") with { StorageType = FeatureStorageMode.RepoCentral };
        WriteSystemCentralRepository(repo, Guid.NewGuid().ToString(), [local]);
        FakeConsoleInteraction console = new([], ["Before (before)", EditFeatureName, "Back"]);
        console.EnqueueValueEdit("After");

        FeatureCommand.Run(new StartupContext(repo, profile, Path.Combine(profile, "soul.md")), ["list"], console, new FakeFeatureAiSessionLauncher());

        Assert.Equal("After", Assert.Single(ProjectDevCraftConfigurationReader.Read(repo)!.Features).Name);
        Assert.Equal("After", Assert.Single(Assert.Single(SystemCentralProjectsStore.Read(profile).Projects).Features).Name);
    }

    [Fact]
    public void List_ProjectsIndexBusy_ReportsAndWritesNothing()
    {
        using TestDirectory root = new();
        string profile = Path.Combine(root.Path, ".DevCraft");
        string repo = Path.Combine(root.Path, "repo");
        WriteSystemCentralRepository(repo, Guid.NewGuid().ToString(), [SampleFeature("Legacy", "legacy", "folder-legacy", "Discovery")]);
        string configBefore = File.ReadAllText(ProjectDevCraftConfigurationStore.PathFor(repo));
        FakeConsoleInteraction console = new([]);

        using (ProjectsIndexLock.Acquire(profile))
        {
            FeatureCommand.Run(new StartupContext(repo, profile, Path.Combine(profile, "soul.md")), ["list"], console, new FakeFeatureAiSessionLauncher());
        }

        Assert.Contains("projects.json is busy in another DevCraft session. Try again.", console.StatusMessages);
        Assert.Equal(configBefore, File.ReadAllText(ProjectDevCraftConfigurationStore.PathFor(repo)));
        Assert.Empty(SystemCentralProjectsStore.Read(profile).Projects);
    }

    [Fact]
    public void EditWorkItem_PrefilledAndSaved()
    {
        using TestDirectory root = new();
        (StartupContext context, string profile) = CentralRepository(root, SampleFeature("Linked", "linked", "folder-linked", "Discovery") with { WorkItemId = "1", ExternalReference = "https://example.com/1" });
        FakeConsoleInteraction console = new([], ["Linked (linked)", EditWorkItem, "Back"]);
        console.EnqueueFieldEdit(new Dictionary<string, string> { ["WorkItemId"] = " PROJ-9 ", ["WorkItemUrl"] = "https://jira.example.com/browse/PROJ-9" });

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher());

        Assert.Equal("Edit Work Item", console.FieldEditTitles[0]);
        Assert.Equal(["1", "https://example.com/1"], console.FieldEditFields[0].Select(field => field.InitialValue));
        DevCraftFeature feature = Assert.Single(Assert.Single(SystemCentralProjectsStore.Read(profile).Projects).Features);
        Assert.Equal(("PROJ-9", "https://jira.example.com/browse/PROJ-9"), (feature.WorkItemId, feature.ExternalReference));
        Assert.Contains("Work item updated for Linked.", console.StatusMessages);
    }

    [Fact]
    public void EditWorkItem_Cancel_NoChange()
    {
        using TestDirectory root = new();
        DevCraftFeature original = SampleFeature("Linked", "linked", "folder-linked", "Discovery") with { WorkItemId = "1" };
        (StartupContext context, string profile) = CentralRepository(root, original);
        FakeConsoleInteraction console = new([], ["Linked (linked)", EditWorkItem, "Back"]);
        console.EnqueueFieldEdit(new Dictionary<string, string> { ["WorkItemId"] = "2", ["WorkItemUrl"] = "" }, saved: false);

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher());

        Assert.Equal(original, Assert.Single(Assert.Single(SystemCentralProjectsStore.Read(profile).Projects).Features));
        Assert.Contains("Work item is unchanged.", console.StatusMessages);
    }

    [Fact]
    public void EditWorkItem_Cleared_SavesNull()
    {
        using TestDirectory root = new();
        (StartupContext context, string profile) = CentralRepository(root, SampleFeature("Linked", "linked", "folder-linked", "Discovery") with { WorkItemId = "1", ExternalReference = "https://example.com/1" });
        FakeConsoleInteraction console = new([], ["Linked (linked)", EditWorkItem, "Back"]);
        console.EnqueueFieldEdit(new Dictionary<string, string> { ["WorkItemId"] = "  ", ["WorkItemUrl"] = "" });

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher());

        DevCraftFeature feature = Assert.Single(Assert.Single(SystemCentralProjectsStore.Read(profile).Projects).Features);
        Assert.Null(feature.WorkItemId);
        Assert.Null(feature.ExternalReference);
    }

    [Fact]
    public void EditWorkItem_InvalidUrl_ReopensForm()
    {
        using TestDirectory root = new();
        (StartupContext context, string profile) = CentralRepository(root, SampleFeature("Linked", "linked", "folder-linked", "Discovery"));
        FakeConsoleInteraction console = new([], ["Linked (linked)", EditWorkItem, "Back"]);
        console.EnqueueFieldEdit(new Dictionary<string, string> { ["WorkItemId"] = "5", ["WorkItemUrl"] = "example.com/5" });
        console.EnqueueFieldEdit(new Dictionary<string, string> { ["WorkItemId"] = "5", ["WorkItemUrl"] = "https://example.com/5" });

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher());

        Assert.Equal(2, console.FieldEditTitles.Count);
        Assert.Equal(["5", "example.com/5"], console.FieldEditFields[1].Select(field => field.InitialValue));
        Assert.Equal("https://example.com/5", Assert.Single(Assert.Single(SystemCentralProjectsStore.Read(profile).Projects).Features).ExternalReference);
    }

    [Fact]
    public void NewFeature_SystemCentral_WorkItemInProjectsJsonOnly()
    {
        using TestDirectory root = new();
        string profile = Path.Combine(root.Path, ".DevCraft");
        string repo = Path.Combine(root.Path, "repo");
        WriteSystemCentralRepository(repo, Guid.NewGuid().ToString(), []);

        FeatureCommand.Run(new StartupContext(repo, profile, Path.Combine(profile, "soul.md")), ["new", "Tracked"], new FakeConsoleInteraction([]), new FakeFeatureAiSessionLauncher(), workItemId: "1234", workItemUrl: "https://example.com/1234");

        Assert.Empty(ProjectDevCraftConfigurationReader.Read(repo)!.Features);
        DevCraftFeature feature = Assert.Single(Assert.Single(SystemCentralProjectsStore.Read(profile).Projects).Features);
        Assert.Equal(("1234", "https://example.com/1234"), (feature.WorkItemId, feature.ExternalReference));
    }

    [Fact]
    public void NewFeature_RepoCentral_WorkItemInBoth()
    {
        using TestDirectory root = new();
        string profile = Path.Combine(root.Path, ".DevCraft");
        string repo = Path.Combine(root.Path, "repo");
        Directory.CreateDirectory(Path.Combine(repo, ".devcraft"));
        ProjectDevCraftConfigurationStore.Write(repo, new ProjectDevCraftConfiguration(Guid.NewGuid().ToString(), FeatureStorageMode.RepoCentral, "features.json", SampleProfile, []));

        FeatureCommand.Run(new StartupContext(repo, profile, Path.Combine(profile, "soul.md")), ["new", "Tracked"], new FakeConsoleInteraction([]), new FakeFeatureAiSessionLauncher(), workItemId: "#57", workItemUrl: null);

        Assert.Equal("#57", Assert.Single(ProjectDevCraftConfigurationReader.Read(repo)!.Features).WorkItemId);
        Assert.Equal("#57", Assert.Single(Assert.Single(SystemCentralProjectsStore.Read(profile).Projects).Features).WorkItemId);
    }

    private static (StartupContext Context, string Profile) CentralRepository(TestDirectory root, params DevCraftFeature[] features)
    {
        string profile = Path.Combine(root.Path, ".DevCraft");
        string repo = Path.Combine(root.Path, "repo");
        string projectKey = Guid.NewGuid().ToString();
        WriteSystemCentralRepository(repo, projectKey, []);
        SystemCentralProjectsStore.Write(profile, new SystemCentralProjectsIndex([CentralProject(projectKey, "Sample Project", "sample-project", repo, features)]));

        return (new StartupContext(repo, profile, Path.Combine(profile, "soul.md")), profile);
    }

    private const string EditWorkItem = "Edit Work Item";
    [Fact]
    public void SetCurrentBranch_LinksBranch()
    {
        using TestDirectory root = new();
        (StartupContext context, string profile) = CentralRepository(root, SampleFeature("Linking", "linking", "folder-linking", "Discovery"));
        FakeConsoleInteraction console = new([], ["Linking (linking)", SetCurrentBranch, "Back"]);

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher(), gitRunner: OnBranch("story/1234-linking"));

        Assert.Equal("story/1234-linking", CentralFeature(profile, "linking").Branch);
        Assert.Contains("Linking is linked to story/1234-linking.", console.StatusMessages);
    }

    [Fact]
    public void SetCurrentBranch_AlreadyLinked_NoWrite()
    {
        using TestDirectory root = new();
        (StartupContext context, string profile) = CentralRepository(root, SampleFeature("Linking", "linking", "folder-linking", "Discovery") with { Branch = "story/1" });
        FakeConsoleInteraction console = new([], ["Linking (linking)", SetCurrentBranch, "Back"]);
        FakeGitRunner git = OnBranch("main").On("symbolic-ref --short -q HEAD", output: "story/1\n");

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher(), gitRunner: git);

        Assert.Contains("Linking is already linked to story/1.", console.StatusMessages);
        Assert.DoesNotContain(console.SelectChoices, choices => choices.SequenceEqual(["No", "Yes"]));
    }

    [Fact]
    public void SetCurrentBranch_ReplaceOwn_Yes_Replaces()
    {
        using TestDirectory root = new();
        (StartupContext context, string profile) = CentralRepository(root, SampleFeature("Linking", "linking", "folder-linking", "Discovery") with { Branch = "story/old" });
        FakeConsoleInteraction console = new([], ["Linking (linking)", SetCurrentBranch, "Yes", "Back"]);

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher(), gitRunner: OnBranch("story/new"));

        Assert.Contains("Replace story/old with story/new?", console.SelectTitles);
        Assert.Equal("story/new", CentralFeature(profile, "linking").Branch);
    }

    [Fact]
    public void SetCurrentBranch_ReplaceOwn_No_Unchanged()
    {
        using TestDirectory root = new();
        (StartupContext context, string profile) = CentralRepository(root, SampleFeature("Linking", "linking", "folder-linking", "Discovery") with { Branch = "story/old" });
        FakeConsoleInteraction console = new([], ["Linking (linking)", SetCurrentBranch, "No", "Back"]);

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher(), gitRunner: OnBranch("story/new"));

        Assert.Equal("story/old", CentralFeature(profile, "linking").Branch);
        Assert.Contains("Branch link is unchanged.", console.StatusMessages);
    }

    [Fact]
    public void SetCurrentBranch_HeldByOther_Yes_MovesAndClearsOther()
    {
        using TestDirectory root = new();
        (StartupContext context, string profile) = CentralRepository(
            root,
            SampleFeature("Holder", "holder", "folder-holder", "Discovery") with { Branch = "story/shared" },
            SampleFeature("Taker", "taker", "folder-taker", "Discovery"));
        FakeConsoleInteraction console = new([], ["Taker (taker)", SetCurrentBranch, "Yes", "Back"]);

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher(), gitRunner: OnBranch("story/shared"));

        Assert.Contains("Move story/shared from Holder to this feature?", console.SelectTitles);
        Assert.Null(CentralFeature(profile, "holder").Branch);
        Assert.Equal("story/shared", CentralFeature(profile, "taker").Branch);
    }

    [Fact]
    public void SetCurrentBranch_HeldByOther_No_Unchanged()
    {
        using TestDirectory root = new();
        (StartupContext context, string profile) = CentralRepository(
            root,
            SampleFeature("Holder", "holder", "folder-holder", "Discovery") with { Branch = "story/shared" },
            SampleFeature("Taker", "taker", "folder-taker", "Discovery"));
        FakeConsoleInteraction console = new([], ["Taker (taker)", SetCurrentBranch, "No", "Back"]);

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher(), gitRunner: OnBranch("story/shared"));

        Assert.Equal("story/shared", CentralFeature(profile, "holder").Branch);
        Assert.Null(CentralFeature(profile, "taker").Branch);
    }

    [Fact]
    public void SetCurrentBranch_DetachedHead_StopsWithNotice()
    {
        using TestDirectory root = new();
        (StartupContext context, string profile) = CentralRepository(root, SampleFeature("Linking", "linking", "folder-linking", "Discovery"));
        FakeConsoleInteraction console = new([], ["Linking (linking)", SetCurrentBranch, "Back"]);

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher(), gitRunner: new FakeGitRunner().On("symbolic-ref --short -q HEAD", exitCode: 1));

        Assert.Contains("HEAD is detached. Switch to a branch first.", console.StatusMessages);
        Assert.Null(CentralFeature(profile, "linking").Branch);
    }

    [Fact]
    public void SetCurrentBranch_NotARepository_StopsWithNotice()
    {
        using TestDirectory root = new();
        (StartupContext context, string profile) = CentralRepository(root, SampleFeature("Linking", "linking", "folder-linking", "Discovery"));
        FakeConsoleInteraction console = new([], ["Linking (linking)", SetCurrentBranch, "Back"]);

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher(), gitRunner: new FakeGitRunner().On("rev-parse --is-inside-work-tree", exitCode: 128));

        Assert.Contains("This folder is not a git repository.", console.StatusMessages);
        Assert.Null(CentralFeature(profile, "linking").Branch);
    }

    [Fact]
    public void SetCurrentBranch_RepoCentral_WritesBoth()
    {
        using TestDirectory root = new();
        string profile = Path.Combine(root.Path, ".DevCraft");
        string repo = Path.Combine(root.Path, "repo");
        WriteSystemCentralRepository(repo, Guid.NewGuid().ToString(), [LocalFeature("Local", "local", "folder-local", "Discovery")]);
        FakeConsoleInteraction console = new([], ["Local (local)", SetCurrentBranch, "Back"]);

        FeatureCommand.Run(new StartupContext(repo, profile, Path.Combine(profile, "soul.md")), ["list"], console, new FakeFeatureAiSessionLauncher(), gitRunner: OnBranch("story/local"));

        Assert.Equal("story/local", Assert.Single(ProjectDevCraftConfigurationReader.Read(repo)!.Features).Branch);
        Assert.Equal("story/local", CentralFeature(profile, "local").Branch);
    }

    [Fact]
    public void ClearBranch_HiddenWhenNoBranch()
    {
        using TestDirectory root = new();
        (StartupContext context, _) = CentralRepository(root, SampleFeature("Linking", "linking", "folder-linking", "Discovery"));
        FakeConsoleInteraction console = new([], ["Linking (linking)", "Back"]);

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher(), gitRunner: OnBranch("main"));

        Assert.DoesNotContain(ClearBranch, console.SelectChoices[console.SelectTitles.IndexOf("Feature actions")]);
    }

    [Fact]
    public void ClearBranch_Confirm_SetsNull()
    {
        using TestDirectory root = new();
        (StartupContext context, string profile) = CentralRepository(root, SampleFeature("Linking", "linking", "folder-linking", "Discovery") with { Branch = "story/1" });
        FakeConsoleInteraction console = new([], ["Linking (linking)", ClearBranch, "Yes", "Back"]);
        FakeGitRunner git = OnBranch("main");

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher(), gitRunner: git);

        Assert.Contains("Clear branch story/1 from this feature?", console.SelectTitles);
        Assert.Null(CentralFeature(profile, "linking").Branch);
        Assert.Contains("Branch story/1 cleared from Linking.", console.StatusMessages);
        Assert.DoesNotContain(git.Calls, call => call.StartsWith("branch -", StringComparison.Ordinal) || call.StartsWith("switch", StringComparison.Ordinal));
    }

    [Fact]
    public void ClearBranch_Decline_Unchanged()
    {
        using TestDirectory root = new();
        (StartupContext context, string profile) = CentralRepository(root, SampleFeature("Linking", "linking", "folder-linking", "Discovery") with { Branch = "story/1" });
        FakeConsoleInteraction console = new([], ["Linking (linking)", ClearBranch, "No", "Back"]);

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher(), gitRunner: OnBranch("main"));

        Assert.Equal("story/1", CentralFeature(profile, "linking").Branch);
    }

    private static FakeGitRunner OnBranch(string branch)
    {
        return new FakeGitRunner().On("symbolic-ref --short -q HEAD", output: branch + "\n");
    }

    private static DevCraftFeature CentralFeature(string profile, string slug)
    {
        return Assert.Single(SystemCentralProjectsStore.Read(profile).Projects).Features.Single(feature => feature.Slug == slug);
    }

    private const string SetCurrentBranch = "Set Feature to Current Branch";
    private const string ClearBranch = "Clear Feature Branch";
    [Fact]
    public void List_CurrentBranchFeature_ListedFirstWithMarker()
    {
        using TestDirectory root = new();
        (StartupContext context, _) = CentralRepository(
            root,
            SampleFeature("Alpha", "alpha", "folder-alpha", "Discovery"),
            SampleFeature("Beta", "beta", "folder-beta", "Implementation") with { Branch = "story/beta" });
        FakeConsoleInteraction console = new([], ["Back"]);

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher(), gitRunner: OnBranch("story/beta"));

        Assert.Equal(
            ["Back", "Completed", "● Beta (beta) (current branch)", "Alpha (alpha)"],
            console.SelectChoices[console.SelectTitles.IndexOf("Select a feature")]);
    }

    [Fact]
    public void List_NoMatchingBranch_DefaultOrderNoMarker()
    {
        using TestDirectory root = new();
        (StartupContext context, _) = CentralRepository(
            root,
            SampleFeature("Alpha", "alpha", "folder-alpha", "Discovery"),
            SampleFeature("Beta", "beta", "folder-beta", "Implementation"));
        FakeConsoleInteraction console = new([], ["Back"]);

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher(), gitRunner: OnBranch("master"));

        Assert.Equal(["Back", "Completed", "Alpha (alpha)", "Beta (beta)"], console.SelectChoices[console.SelectTitles.IndexOf("Select a feature")]);
    }

    [Fact]
    public void List_DetachedHead_DefaultOrder()
    {
        using TestDirectory root = new();
        (StartupContext context, _) = CentralRepository(
            root,
            SampleFeature("Alpha", "alpha", "folder-alpha", "Discovery"),
            SampleFeature("Beta", "beta", "folder-beta", "Implementation") with { Branch = "story/beta" });
        FakeConsoleInteraction console = new([], ["Back"]);

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher(), gitRunner: new FakeGitRunner().On("symbolic-ref --short -q HEAD", exitCode: 1));

        Assert.Equal(["Back", "Completed", "Alpha (alpha)", "Beta (beta)"], console.SelectChoices[console.SelectTitles.IndexOf("Select a feature")]);
    }

    [Fact]
    public void List_ShowsCheckedOutInLabels()
    {
        using TestDirectory root = new();
        string profile = Path.Combine(root.Path, ".DevCraft");
        string projectKey = Guid.NewGuid().ToString();
        string first = Path.Combine(root.Path, "Active Work 1", "ProjectCodeArc");
        string second = Path.Combine(root.Path, "Active Work 2", "ProjectCodeArc");
        Directory.CreateDirectory(first);
        WriteSystemCentralRepository(second, projectKey, []);
        SystemCentralProjectsStore.Write(profile, new SystemCentralProjectsIndex(
        [
            CentralProject(projectKey, "Code Arc", "code-arc", first, [SampleFeature("Linking", "linking", "folder-linking", "Discovery") with { Branch = "story/1" }])
                with { WorkingCopies = [new WorkingCopy(first, "story/1", null)] },
        ]));
        FakeConsoleInteraction console = new([], ["Back"]);

        FeatureCommand.Run(new StartupContext(second, profile, Path.Combine(profile, "soul.md")), ["list"], console, new FakeFeatureAiSessionLauncher(), gitRunner: OnBranch("main").On("remote get-url origin", output: "git@github.com:Org/ProjectCodeArc.git"));

        Assert.Contains("Linking (linking) → Active Work 1", console.SelectChoices[console.SelectTitles.IndexOf("Select a feature")]);
    }

    [Fact]
    public void List_MissingCopy_ShowsMissingSuffix()
    {
        using TestDirectory root = new();
        string profile = Path.Combine(root.Path, ".DevCraft");
        string projectKey = Guid.NewGuid().ToString();
        string gone = Path.Combine(root.Path, "Active Work 3", "ProjectCodeArc");
        string current = Path.Combine(root.Path, "Active Work 2", "ProjectCodeArc");
        WriteSystemCentralRepository(current, projectKey, []);
        SystemCentralProjectsStore.Write(profile, new SystemCentralProjectsIndex(
        [
            CentralProject(projectKey, "Code Arc", "code-arc", gone, [SampleFeature("Linking", "linking", "folder-linking", "Discovery") with { Branch = "story/3" }])
                with { WorkingCopies = [new WorkingCopy(gone, "story/3", null)] },
        ]));
        FakeConsoleInteraction console = new([], ["Back"]);

        FeatureCommand.Run(new StartupContext(current, profile, Path.Combine(profile, "soul.md")), ["list"], console, new FakeFeatureAiSessionLauncher(), gitRunner: OnBranch("main").On("remote get-url origin", output: "git@github.com:Org/ProjectCodeArc.git"));

        Assert.Contains("Linking (linking) → Active Work 3 (missing)", console.SelectChoices[console.SelectTitles.IndexOf("Select a feature")]);
    }

    [Fact]
    public void List_SelectingMarkedChoice_OpensThatFeature()
    {
        using TestDirectory root = new();
        (StartupContext context, _) = CentralRepository(root, SampleFeature("Beta", "beta", "folder-beta", "Implementation") with { Branch = "story/beta" });
        FakeConsoleInteraction console = new([], ["● Beta (beta) (current branch)", "Back"]);

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher(), gitRunner: OnBranch("story/beta"));

        Assert.Contains("Selected feature: Beta", console.StatusMessages);
    }

    [Fact]
    public void List_BranchCompare_IsCaseSensitive()
    {
        using TestDirectory root = new();
        (StartupContext context, _) = CentralRepository(root, SampleFeature("Beta", "beta", "folder-beta", "Implementation") with { Branch = "Story/Beta" });
        FakeConsoleInteraction console = new([], ["Back"]);

        FeatureCommand.Run(context, ["list"], console, new FakeFeatureAiSessionLauncher(), gitRunner: OnBranch("story/beta"));

        Assert.Equal(["Back", "Completed", "Beta (beta)"], console.SelectChoices[console.SelectTitles.IndexOf("Select a feature")]);
    }

    [Theory]
    [InlineData("list")]
    [InlineData("new")]
    public void KeylessConfig_ListAndNew_ReportAndDoNotThrow(string command)
    {
        using TestDirectory root = new();
        string profile = Path.Combine(root.Path, ".DevCraft");
        string repo = Path.Combine(root.Path, "repo");
        Directory.CreateDirectory(Path.Combine(repo, ".devcraft"));
        File.WriteAllText(ProjectDevCraftConfigurationStore.PathFor(repo), """{ "SelectedFeatureStorage": "system-central" }""");
        FakeConsoleInteraction console = new([]);
        FakeFeatureAiSessionLauncher launcher = new();

        FeatureCommand.Run(new StartupContext(repo, profile, Path.Combine(profile, "soul.md")), [command, "Thing"], console, launcher, gitRunner: new FakeGitRunner());

        Assert.Contains("This folder's .devcraft/configure.json has no ProjectKey. Run `devcraft -force` here to repair it.", console.StatusMessages);
        Assert.Empty(launcher.Launches);
        Assert.Equal("""{ "SelectedFeatureStorage": "system-central" }""", File.ReadAllText(ProjectDevCraftConfigurationStore.PathFor(repo)));
    }

    private const string EditFeatureName = "Edit Feature Name";
    private const string OpenFeatureDirectory = "Open Feature Directory using Code";
    private const string StartConversation = "Start a conversation about the feature";

    private static DevCraftFeature SampleFeature(string name, string slug, string folderName, string status)
    {
        return new DevCraftFeature(name, slug, $"{name} description.", folderName, FeatureStorageMode.SystemCentral, null, status);
    }

    private static DevCraftFeature LocalFeature(string name, string slug, string folderName, string status)
    {
        return SampleFeature(name, slug, folderName, status) with { StorageType = FeatureStorageMode.RepoCentral };
    }

    private static void WriteSystemCentralRepository(string repositoryPath, string projectKey, IReadOnlyList<DevCraftFeature> features)
    {
        Directory.CreateDirectory(Path.Combine(repositoryPath, ".devcraft"));
        ProjectDevCraftConfigurationStore.Write(
            repositoryPath,
            new ProjectDevCraftConfiguration(
                projectKey,
                FeatureStorageMode.SystemCentral,
                "features.json",
                SampleProfile,
                features));
    }

    [Fact]
    public void ReadsLegacySystemCentralProjectIndexJson()
    {
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(Path.Combine(profile, "features"));
        string projectKey = Guid.NewGuid().ToString();
        File.WriteAllText(
            Path.Combine(profile, "features", "projects.json"),
            $$"""
            {
              "Projects": [
                {
                  "ProjectKey": "{{projectKey}}",
                  "Name": "Legacy Project",
                  "Slug": "legacy-project",
                  "RepositoryPath": "/tmp/legacy-project",
                  "Features": [
                    {
                      "Name": "Legacy Feature",
                      "Slug": "legacy-feature",
                      "ShortDescription": "Legacy description.",
                      "FolderName": "legacy-folder",
                      "StorageType": "github-issue",
                      "ExternalReference": "GH-99"
                    }
                  ]
                }
              ]
            }
            """);

        SystemCentralProject project = Assert.Single(SystemCentralProjectsStore.Read(profile).Projects);

        Assert.Equal(projectKey, project.ProjectKey);
        Assert.Equal("/tmp/legacy-project", Assert.Single(project.WorkingCopies).Path);
        Assert.Equal("legacy-project", project.RepositoryName);
        DevCraftFeature feature = Assert.Single(project.Features);
        Assert.Equal("Legacy Feature", feature.Name);
        Assert.Equal("Legacy description.", feature.ShortDescription);
        Assert.Equal("GH-99", feature.ExternalReference);
        Assert.Equal("Discovery", feature.Status);
    }

    private static SystemCentralProject CentralProject(
        string projectKey,
        string name,
        string slug,
        string repositoryPath,
        IReadOnlyList<DevCraftFeature> features)
    {
        return new SystemCentralProject(
            projectKey,
            name,
            slug,
            SystemCentralProject.FolderName(repositoryPath),
            null,
            [new WorkingCopy(repositoryPath, null, null)],
            features);
    }
}
