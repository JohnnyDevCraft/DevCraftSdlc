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
        Assert.Equal("Central Feature", Assert.Single(project.Features).Name);
        Assert.True(Directory.Exists(Path.Combine(profile, "features", feature.FolderName)));
    }
}
