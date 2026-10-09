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
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, soul);
        FakeConsoleInteraction console = new([], ["repo central (repo-central)"]);
        FakeFeatureAiSessionLauncher launcher = new();

        FeatureCommand.Run(context, ["new", "First", "Feature"], console, launcher);

        ProjectDevCraftConfiguration configuration = ProjectDevCraftConfigurationReader.Read(root.Path)!;
        DevCraftFeature feature = Assert.Single(configuration.Features);
        Assert.Equal("First Feature", feature.Name);
        Assert.Equal("first-feature", feature.Slug);
        Assert.Equal("repo-central", feature.StorageType);
        Assert.Contains(console.StatusMessages, message => message == "Feature storage set for this repository: repo-central");
        Assert.True(Directory.Exists(Path.Combine(root.Path, ".devcraft", "features", feature.FolderName)));
        Assert.Single(launcher.Launches);
        Assert.Equal("Create a new feature.", launcher.Launches[0].Instruction);
    }

    [Fact]
    public void ListPromptsForFeatureStorageWhenRepositoryHasNoSelection()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        string soul = Path.Combine(profile, "soul.md");
        Directory.CreateDirectory(profile);
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
        FakeConsoleInteraction console = new([], ["repo central (repo-central)", "Pending Storage Feature (pending-storage-feature)"]);
        FakeFeatureAiSessionLauncher launcher = new();

        FeatureCommand.Run(context, ["list"], console, launcher);

        ProjectDevCraftConfiguration configuration = ProjectDevCraftConfigurationReader.Read(root.Path)!;
        Assert.Equal("repo-central", configuration.SelectedFeatureStorage);
        Assert.Contains(console.StatusMessages, message => message == "Feature storage set for this repository: repo-central");
        Assert.Single(launcher.Launches);
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
