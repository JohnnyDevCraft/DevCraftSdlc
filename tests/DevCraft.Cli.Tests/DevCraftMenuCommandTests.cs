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
        FakeConsoleInteraction console = new([], ["Project discovery", "OpenAI Codex (codex)", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        string instruction = Assert.Single(launcher.Instructions);
        Assert.Contains("project discovery", instruction, StringComparison.OrdinalIgnoreCase);
        Assert.True(console.MenuShellCount > 0);
        Assert.Empty(featureLauncher.Launches);
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
        FakeConsoleInteraction console = new([], ["Project discovery", "OpenAI Codex (codex)", "Exit"]);
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
        FakeConsoleInteraction console = new([], ["Project discovery", "OpenAI Codex (codex)", "Exit"]);
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
    public void RunFeatureMenuCreatesNewFeature()
    {
        using TestDirectory root = new();
        using TestDirectory profileRoot = new();
        string profile = Path.Combine(profileRoot.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        ProjectDevCraftConfigurationWriter.WriteIfMissing(Path.Combine(root.Path, ".devcraft"), SampleProfile);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));
        FakeConsoleInteraction console = new(["Menu Feature"], ["Features", "Create new feature", "OpenAI Codex (codex)", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        ProjectDevCraftConfiguration configuration = ProjectDevCraftConfigurationReader.Read(root.Path)!;
        DevCraftFeature feature = Assert.Single(configuration.Features);
        Assert.Equal("Menu Feature", feature.Name);
        Assert.Empty(launcher.Instructions);
        Assert.Single(featureLauncher.Launches);
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
        FakeConsoleInteraction console = new([], ["Create Project", "Web API (web-api)", "OpenAI Codex (codex)", "Exit"]);
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
        FakeConsoleInteraction console = new([], ["Create Project", "Project discovery", "OpenAI Codex (codex)", "Exit"]);
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
        FakeConsoleInteraction console = new([], ["Create Project", "Back", "Exit"]);
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
}
