using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class DevCraftRepositoryUpgradePromptBuilderTests
{
    [Fact]
    public void BuildIncludesEffectiveStorageAndStructuredResultContract()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        ProjectDevCraftConfiguration projectConfiguration = new(
            Guid.NewGuid().ToString(),
            FeatureStorageMode.SystemCentral,
            "features.json",
            new ProjectProfile("Sample", "Purpose", "Description"),
            []);
        DevCraftRepositoryUpgradeRequest request = new(
            context,
            projectConfiguration,
            "1.0.0-beta.15",
            "1.0.0-beta.10",
            FeatureStorageMode.SystemCentral);

        string prompt = DevCraftRepositoryUpgradePromptBuilder.Build(request);

        Assert.Contains("non-interactive", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Effective feature storage for this repository: system-central", prompt);
        Assert.Contains(ProjectDevCraftConfigurationStore.PathFor(root.Path), prompt);
        Assert.Contains(SystemCentralProjectsStore.PathFor(profile), prompt);
        Assert.Contains("\"success\": true", prompt);
        Assert.Contains("repository .devcraft/configure.json feature history", prompt);
        Assert.Contains("If repository .devcraft/configure.json is missing or invalid, recreate it", prompt);
        Assert.Contains("profile .DevCraft/features/projects.json feature history", prompt);
        Assert.Contains(".devcraft/features/<guid>", prompt);
        Assert.Contains(".DevCraft/features/<guid>", prompt);
        Assert.Contains("feature browsing", prompt, StringComparison.OrdinalIgnoreCase);
    }
}
