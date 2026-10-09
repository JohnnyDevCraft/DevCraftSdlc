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
        FakeConsoleInteraction console = new(["Menu Feature"], ["Features", "Create new feature", "repo central (repo-central)", "OpenAI Codex (codex)", "Exit"]);
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
    public void RunConfigureDevCraftChangeFeatureStorageUpdatesRepositoryOnly()
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
        FakeConsoleInteraction console = new([], ["Configure DevCraft", "Change Feature Storage", "system central (system-central)", "Exit"]);
        FakeDevCraftAiSessionLauncher launcher = new();
        FakeFeatureAiSessionLauncher featureLauncher = new();

        DevCraftMenuCommand.Run(context, console, launcher, featureLauncher);

        ProjectDevCraftConfiguration repositoryConfiguration = ProjectDevCraftConfigurationReader.Read(root.Path)!;
        DevCraftProfileConfiguration profileConfiguration = ProfileConfigurationReader.Read(profile);
        Assert.Equal("system-central", repositoryConfiguration.SelectedFeatureStorage);
        Assert.DoesNotContain("\"SelectedFeatureStorage\"", File.ReadAllText(Path.Combine(profile, "configure.json")));
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
            ["mongodb://localhost:27017/DevCraft"],
            ["Configure DevCraft", "Configure Situational Awareness", "Yes", "Months & Weeks", "Database", "Exit"]);
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
            ["Ada", "Lovelace", "ada@example.com", "555-0100", "Principal Engineer", "Platform", "Analytical Engines", "Trusted collaborator"],
            ["Logging", "Manage People", "Add Person", "ACTIVE", "Back", "Exit"]);
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
            ["Ada", "Byron", "2/2/2026"],
            ["Logging", "Manage People", "List People", "Ada Lovelace (ada@example.com) | Architect | Math | Analytical Engines", "Edit Name", "Make Inactive", "Go Back", "Go Back", "Back", "Exit"]);
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
