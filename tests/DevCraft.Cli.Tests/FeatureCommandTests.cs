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
        FakeConsoleInteraction console = new([], ["Pending Storage Feature (pending-storage-feature)"]);
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
            selectClient: () =>
            {
                clientSelected = true;
                return SupportedTerminalClientCatalog.Create()[0];
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
        FakeConsoleInteraction console = new([], ["Deferred Client Feature (deferred-client-feature)"]);
        FakeFeatureAiSessionLauncher launcher = new();
        SupportedTerminalClient selectedClient = SupportedTerminalClientCatalog.Create()[1];
        bool featureSelectedBeforeClient = false;

        FeatureCommand.Run(
            context,
            ["list"],
            console,
            launcher,
            selectClient: () =>
            {
                featureSelectedBeforeClient = console.StatusMessages.Contains("Selected feature: Deferred Client Feature");
                return selectedClient;
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
        FakeConsoleInteraction console = new([]);
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
        Assert.Contains(project.Features, feature => feature.Slug == "second-existing" && feature.Status == "Complete");
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
        Assert.Equal("Implementation", projects[1].Features[1].Status);
    }

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
