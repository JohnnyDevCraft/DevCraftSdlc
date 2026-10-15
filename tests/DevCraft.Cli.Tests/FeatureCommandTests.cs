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

        ProjectDevCraftConfiguration configuration = ProjectDevCraftConfigurationReader.Read(root.Path)!;
        DevCraftFeature feature = Assert.Single(configuration.Features);
        SystemCentralProjectsIndex index = SystemCentralProjectsStore.Read(profile);
        SystemCentralProject project = Assert.Single(index.Projects);
        Assert.Equal(projectKey, project.ProjectKey);
        Assert.Equal(root.Path, project.RepositoryPath);
        Assert.Equal(Path.GetFileName(root.Path), project.RepositoryName);
        DevCraftFeature trackedFeature = Assert.Single(project.Features);
        Assert.Equal("Central Feature", trackedFeature.Name);
        Assert.Equal("Discovery", trackedFeature.Status);
        string projectsJson = File.ReadAllText(Path.Combine(profile, "features", "projects.json"));
        Assert.Contains("\"id\":", projectsJson);
        Assert.Contains("\"repo-location\":", projectsJson);
        Assert.Contains("\"repo-name\":", projectsJson);
        Assert.Contains("\"feature-name\": \"Central Feature\"", projectsJson);
        Assert.Contains("\"work-item-id\": null", projectsJson);
        Assert.Contains("\"status\": \"Discovery\"", projectsJson);
        Assert.True(Directory.Exists(Path.Combine(profile, "features", feature.FolderName)));
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
                new SystemCentralProject(
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
        Assert.Contains(
            ProjectDevCraftConfigurationReader.Read(root.Path)!.Features,
            feature => feature.Slug == "second-existing" && feature.Status == "Discovery");
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
    public void SyncPreservesCentralOnlyFeaturesAndOtherProjects()
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
        SystemCentralProject otherProject = new(
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
                new SystemCentralProject(
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
        Assert.Equal(root.Path, projects[1].RepositoryPath);
        Assert.Equal(["central-only", "shared"], projects[1].Features.Select(feature => feature.Slug));
        Assert.Equal("Discovery", projects[1].Features[1].Status);
    }

    [Fact]
    public void SyncCarriesCentralStatusIntoRepositoryConfiguration()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string projectKey = Guid.NewGuid().ToString();
        WriteSystemCentralRepository(
            root.Path,
            projectKey,
            [SampleFeature("Shipped", "shipped", "folder-shipped", "Implementation")]);
        SystemCentralProjectsStore.Write(
            profile,
            new SystemCentralProjectsIndex(
            [
                new SystemCentralProject(
                    projectKey,
                    "Sample Project",
                    "sample-project",
                    root.Path,
                    [SampleFeature("Shipped", "shipped", "folder-shipped", "Complete")]),
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
        Assert.Equal([EditFeatureName, OpenFeatureDirectory, StartConversation, "Commit Code", "Switch and Pull", "Create Branch", "Merge From", "Back"], console.SelectChoices[actionsIndex]);
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
                SampleFeature("First Existing", "first-existing", "folder-one", "Implementation"),
                SampleFeature("Second Existing", "second-existing", "folder-two", "Complete"),
            ]);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new([], ["First Existing (first-existing)", EditFeatureName, "Back"]);
        console.EnqueueValueEdit("  Renamed Feature  ");
        FakeFeatureAiSessionLauncher launcher = new();

        FeatureCommand.Run(context, ["list"], console, launcher);

        DevCraftFeature expected = SampleFeature("First Existing", "first-existing", "folder-one", "Implementation") with
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
        SystemCentralProject otherProject = new(
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
                new SystemCentralProject(
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
            [SampleFeature("First Existing", "first-existing", "folder-one", "Implementation")]);
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
            [SampleFeature("First Existing", "first-existing", "folder-one", "Implementation")]);
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
            [SampleFeature("First Existing", "first-existing", "folder-one", "Implementation")]);
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
            [SampleFeature("First Existing", "first-existing", "folder-one", "Implementation")]);
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

    private const string EditFeatureName = "Edit Feature Name";
    private const string OpenFeatureDirectory = "Open Feature Directory using Code";
    private const string StartConversation = "Start a conversation about the feature";

    private static DevCraftFeature SampleFeature(string name, string slug, string folderName, string status)
    {
        return new DevCraftFeature(name, slug, $"{name} description.", folderName, FeatureStorageMode.SystemCentral, null, status);
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
        Assert.Equal("/tmp/legacy-project", project.RepositoryPath);
        Assert.Equal("legacy-project", project.RepositoryName);
        DevCraftFeature feature = Assert.Single(project.Features);
        Assert.Equal("Legacy Feature", feature.Name);
        Assert.Equal("Legacy description.", feature.ShortDescription);
        Assert.Equal("GH-99", feature.ExternalReference);
        Assert.Equal("Discovery", feature.Status);
    }
}
