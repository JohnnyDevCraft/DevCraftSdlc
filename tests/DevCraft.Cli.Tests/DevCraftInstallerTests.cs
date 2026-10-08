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

    [Fact]
    public void InstallUsesProfileTemplatesWhenAvailable()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profileDirectory = Path.Combine(profileRoot.Path, ".DevCraft");
        string templatesDirectory = Path.Combine(profileDirectory, "templates");
        Directory.CreateDirectory(templatesDirectory);
        File.WriteAllText(
            Path.Combine(templatesDirectory, "README.template.md"),
            """
            # Project Name

            ## Overview

            Describe the project, what it does, and who it serves.
            """);
        File.WriteAllText(
            Path.Combine(templatesDirectory, "DISCOVERY.template.md"),
            """
            # Discovery

            - Core project purpose:
            - Primary problem solved:
            - MVP outcome:
            """);
        ProjectProfile profile = new(
            "Template Project",
            "Build from profile templates.",
            "Project description from profile.");

        DevCraftInstaller.Install(root.Path, profile, profileDirectory);

        string readme = File.ReadAllText(Path.Combine(root.Path, ".devcraft", "README.md"));
        string discovery = File.ReadAllText(Path.Combine(root.Path, ".devcraft", "DISCOVERY.md"));
        Assert.Contains("# Template Project", readme);
        Assert.Contains("Project description from profile.", readme);
        Assert.Contains("- Core project purpose: Build from profile templates.", discovery);
        Assert.Contains("- Primary problem solved: Project description from profile.", discovery);
        Assert.True(File.Exists(Path.Combine(root.Path, ".devcraft", "DESIGN.md")));
    }
}
