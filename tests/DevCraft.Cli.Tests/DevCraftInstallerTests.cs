using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class DevCraftInstallerTests
{
    [Fact]
    public void InstallCreatesRepositoryConfigureJson()
    {
        using TestDirectory root = new();
        ProjectProfile profile = new(
            "Sample Project",
            "Validate repository configuration.",
            "Sample project description.");

        DevCraftInstaller.Install(root.Path, profile);

        string configurePath = Path.Combine(root.Path, ".devcraft", "configure.json");
        string configureJson = File.ReadAllText(configurePath);
        Assert.Contains("\"ProjectKey\"", configureJson);
        Assert.Contains("\"SelectedFeatureStorage\"", configureJson);
        Assert.Contains("\"repo-central\"", configureJson);
        Assert.Contains("\"FeaturesIndexPath\"", configureJson);
        Assert.Contains("\"features.json\"", configureJson);
        Assert.Contains("\"Features\"", configureJson);
        Assert.Contains("\"Sample Project\"", configureJson);
    }
}
