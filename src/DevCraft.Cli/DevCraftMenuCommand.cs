namespace DevCraft.Cli;

public static class DevCraftMenuCommand
{
    public static Func<string, bool> CommandExists { get; set; } = CommandLocator.Exists;

    private const string ProjectDiscovery = "Project discovery";
    private const string ProjectDesign = "Project design";
    private const string ProjectTheme = "Project theme";
    private const string ProjectSetup = "Project setup";
    private const string CreateProject = "Create Project";
    private const string Features = "Features";
    private const string Skills = "Skills";
    private const string ConfigureDevCraft = "Configure DevCraft";
    private const string Back = "Back";
    private const string Exit = "Exit";
    private const string ListFeatures = "List features";
    private const string NewFeature = "Create new feature";
    private const string ImportSettings = "Import Settings";
    private const string ChangeFeatureStorage = "Change Feature Storage";
    private const string CreateSkill = "Create Skill";
    private const string CreateStandards = "Create Standards";
    private const string CreateArchitecture = "Create Architecture";
    private const string CreateProjectType = "Create Project Type";

    public static void Run(
        StartupContext context,
        IConsoleInteraction console,
        IDevCraftAiSessionLauncher launcher,
        IFeatureAiSessionLauncher featureLauncher)
    {
        ProjectDevCraftConfiguration? projectConfiguration = ProjectDevCraftConfigurationReader.Read(context.CurrentDirectory);

        if (projectConfiguration is null)
        {
            return;
        }

        string? notice = null;

        while (true)
        {
            projectConfiguration = ProjectDevCraftConfigurationReader.Read(context.CurrentDirectory);

            if (projectConfiguration is null)
            {
                return;
            }

            console.ShowMenuShell();
            string title = notice is null
                ? "What do you want to work on?"
                : $"{notice}{Environment.NewLine}{Environment.NewLine}What do you want to work on?";
            notice = null;
            string selected = console.Select(
                title,
                [ProjectDiscovery, ProjectDesign, ProjectTheme, ProjectSetup, CreateProject, Features, Skills, ConfigureDevCraft, Exit]);

            switch (selected)
            {
                case Exit:
                    return;
                case ProjectDiscovery:
                    notice = LaunchClient(
                        () => launcher.Launch(
                            context,
                            projectConfiguration,
                            SelectAiClient(context, console),
                            "Work on project discovery. Use and maintain the repository DevCraft discovery artifact for this project."));
                    break;
                case ProjectDesign:
                    notice = LaunchClient(
                        () => launcher.Launch(
                            context,
                            projectConfiguration,
                            SelectAiClient(context, console),
                            "Work on project design and component design. Use and maintain the repository DevCraft design artifact for this project."));
                    break;
                case ProjectTheme:
                    notice = LaunchClient(
                        () => launcher.Launch(
                            context,
                            projectConfiguration,
                            SelectAiClient(context, console),
                            "Work on project theme. Use and maintain the repository DevCraft theme artifact for this project."));
                    break;
                case ProjectSetup:
                    notice = LaunchClient(
                        () => launcher.Launch(
                            context,
                            projectConfiguration,
                            SelectAiClient(context, console),
                            "Work on project setup. Use and maintain the repository DevCraft setup context for this project."));
                    break;
                case CreateProject:
                    string? createProjectNotice = RunCreateProject(context, console, launcher, projectConfiguration);

                    if (createProjectNotice is null)
                    {
                        break;
                    }

                    notice = createProjectNotice;
                    break;
                case Features:
                    notice = LaunchClient(() => RunFeatureMenu(context, console, featureLauncher));
                    break;
                case Skills:
                    notice = RunSkillsMenu(context, console, launcher, projectConfiguration);
                    break;
                case ConfigureDevCraft:
                    notice = LaunchClient(() => RunConfigureDevCraft(context, console, launcher, projectConfiguration));
                    break;
            }
        }
    }

    private static string? RunCreateProject(
        StartupContext context,
        IConsoleInteraction console,
        IDevCraftAiSessionLauncher launcher,
        ProjectDevCraftConfiguration projectConfiguration)
    {
        DevCraftProfileConfiguration profileConfiguration = ProfileConfigurationReader.Read(context.ProfileDirectory);

        if (profileConfiguration.ProjectTypes.Count == 0)
        {
            return "There are no project types in the catalog. Please add a project type and try again.";
        }

        console.ShowMenuShell();
        string selected = console.Select(
            "Select a project type",
            profileConfiguration.ProjectTypes.Select(ProjectTypeChoice).Append(Back).ToList());

        if (selected == Back)
        {
            return null;
        }

        ProfileCatalogDocument projectType = profileConfiguration.ProjectTypes.First(item => selected.EndsWith($"({item.Slug})", StringComparison.Ordinal));
        string projectTypePath = Path.Combine(context.ProfileDirectory, projectType.Path);

        return LaunchClient(
            () => launcher.Launch(
                context,
                projectConfiguration,
                SelectAiClient(context, console),
                $"""
                Help the user create a project using the selected DevCraft project type.

                Selected project type:
                - Name: {projectType.Name}
                - Slug: {projectType.Slug}
                - Description: {projectType.Description}
                - Project type file: {projectTypePath}

                Read the selected project type Markdown file before making recommendations or creating project files.
                """));
    }

    private static string ProjectTypeChoice(ProfileCatalogDocument projectType)
    {
        return $"{projectType.Name} ({projectType.Slug})";
    }

    private static void RunConfigureDevCraft(
        StartupContext context,
        IConsoleInteraction console,
        IDevCraftAiSessionLauncher launcher,
        ProjectDevCraftConfiguration projectConfiguration)
    {
        console.ShowMenuShell();
        string selected = console.Select(
            "Configure DevCraft",
            [ImportSettings, ChangeFeatureStorage, CreateSkill, CreateStandards, CreateArchitecture, CreateProjectType, Back]);

        switch (selected)
        {
            case Back:
                return;
            case ImportSettings:
                RunImportSettings(context, console, launcher, projectConfiguration);
                break;
            case ChangeFeatureStorage:
                FeatureStorageSelector.Change(context, console, projectConfiguration);
                break;
            case CreateSkill:
                LaunchProfileCreationSkill(
                    context,
                    console,
                    launcher,
                    projectConfiguration,
                    "Create Skill",
                    "skills/Create-Skill.md",
                    "templates/skill-template.md",
                    "Design a new DevCraft skill with the user, write it to the profile skills folder, and update profile configure.json.");
                break;
            case CreateStandards:
                LaunchProfileCreationSkill(
                    context,
                    console,
                    launcher,
                    projectConfiguration,
                    "Create Standard",
                    "skills/Create-Standard.md",
                    "templates/standard-template.md",
                    "Design a new DevCraft standard with the user, write it to the profile standards folder, and update profile configure.json.");
                break;
            case CreateArchitecture:
                LaunchProfileCreationSkill(
                    context,
                    console,
                    launcher,
                    projectConfiguration,
                    "Create Architecture",
                    "skills/Create-Architecture.md",
                    "templates/architecture-template.md",
                    "Design a new DevCraft architecture with the user, write it to the profile architectures folder, and update profile configure.json.");
                break;
            case CreateProjectType:
                LaunchProfileCreationSkill(
                    context,
                    console,
                    launcher,
                    projectConfiguration,
                    "Create Project Type",
                    "skills/Create-Project-Type.md",
                    "templates/project-type-template.md",
                    "Design a new DevCraft project type with the user, write it to the profile project-types folder, and update profile configure.json.");
                break;
        }
    }

    private static void RunImportSettings(
        StartupContext context,
        IConsoleInteraction console,
        IDevCraftAiSessionLauncher launcher,
        ProjectDevCraftConfiguration projectConfiguration)
    {
        console.ShowMenuShell();
        string importPath = console.Ask("What folder do you want to import from?");
        string importSkillPath = Path.Combine(context.ProfileDirectory, "skills", "DevCraft-Import.md");

        launcher.Launch(
            context,
            projectConfiguration,
            SelectAiClient(context, console),
            $"""
            Import DevCraft settings from a local documentation repository into the profile-level DevCraft folder.

            Import source folder: {importPath}
            DevCraft Import skill: {importSkillPath}
            Profile DevCraft folder: {context.ProfileDirectory}
            Profile configure file: {Path.Combine(context.ProfileDirectory, "configure.json")}

            Read the DevCraft Import skill first. Then inspect the import source folder for these folders:
            - standards
            - architectures
            - project-types
            - skills

            Copy valid Markdown files into the matching profile-level DevCraft folders and update profile configure.json with every imported item.
            """);
    }

    private static void LaunchProfileCreationSkill(
        StartupContext context,
        IConsoleInteraction console,
        IDevCraftAiSessionLauncher launcher,
        ProjectDevCraftConfiguration projectConfiguration,
        string skillName,
        string skillRelativePath,
        string templateRelativePath,
        string task)
    {
        string skillPath = Path.Combine(context.ProfileDirectory, skillRelativePath);
        string templatePath = Path.Combine(context.ProfileDirectory, templateRelativePath);

        launcher.Launch(
            context,
            projectConfiguration,
            SelectAiClient(context, console),
            $"""
            Configure DevCraft: {skillName}.

            Task: {task}

            Skill file: {skillPath}
            Template file: {templatePath}
            Profile DevCraft folder: {context.ProfileDirectory}
            Profile configure file: {Path.Combine(context.ProfileDirectory, "configure.json")}
            Repository configure file: {ProjectDevCraftConfigurationStore.PathFor(context.CurrentDirectory)}
            Current repository: {context.CurrentDirectory}

            Read the three core DevCraft files, the skill file, and the template file before asking questions or writing files.

            Before creating the new item, ask the user where it should be created:
            - Current repository: create it in this repository as reusable library content for other people to use later.
            - Profile DevCraft folder: create it in the profile-level DevCraft folder and update the profile configure.json catalog immediately.

            If the user chooses the current repository, use the matching repository library folder for the item type and do not silently add it to the profile catalog.
            If the user chooses the profile DevCraft folder, write it to the matching profile folder and update profile configure.json with the new slug, name, description, and path.
            """);
    }

    private static void RunFeatureMenu(
        StartupContext context,
        IConsoleInteraction console,
        IFeatureAiSessionLauncher featureLauncher)
    {
        console.ShowMenuShell();
        string selected = console.Select("Feature options", [ListFeatures, NewFeature, Back]);

        if (selected == Back)
        {
            return;
        }

        if (selected == ListFeatures)
        {
            ProjectDevCraftConfiguration? projectConfiguration = ProjectDevCraftConfigurationReader.Read(context.CurrentDirectory);

            if (projectConfiguration is null)
            {
                return;
            }

            FeatureStorageSelector.EnsureSelected(context, console, projectConfiguration);
            FeatureCommand.Run(context, ["list"], console, featureLauncher, SelectAiClient(context, console));
            return;
        }

        console.ShowMenuShell();
        string featureName = console.Ask("What is the feature name?");
        ProjectDevCraftConfiguration? configuration = ProjectDevCraftConfigurationReader.Read(context.CurrentDirectory);

        if (configuration is null)
        {
            return;
        }

        FeatureStorageSelector.EnsureSelected(context, console, configuration);
        FeatureCommand.Run(context, ["new", featureName], console, featureLauncher, SelectAiClient(context, console));
    }

    private static string? RunSkillsMenu(
        StartupContext context,
        IConsoleInteraction console,
        IDevCraftAiSessionLauncher launcher,
        ProjectDevCraftConfiguration projectConfiguration)
    {
        DevCraftProfileConfiguration profileConfiguration = ProfileConfigurationReader.Read(context.ProfileDirectory);

        if (profileConfiguration.Skills.Count == 0)
        {
            return "There are no skills in the catalog. Please add a skill and try again.";
        }

        console.ShowMenuShell();
        string selected = console.Select(
            "Select a skill",
            profileConfiguration.Skills.Select(SkillChoice).Append(Back).ToList());

        if (selected == Back)
        {
            return null;
        }

        ProfileCatalogDocument skill = profileConfiguration.Skills.First(item => selected.EndsWith($"({item.Slug})", StringComparison.Ordinal));
        string skillPath = Path.Combine(context.ProfileDirectory, skill.Path);

        return LaunchClient(
            () => launcher.Launch(
                context,
                projectConfiguration,
                SelectAiClient(context, console),
                $"""
                Use the selected DevCraft skill with the user.

                Selected skill:
                - Name: {skill.Name}
                - Slug: {skill.Slug}
                - Description: {skill.Description}
                - Skill file: {skillPath}

                Read the selected skill file before acting. Start by telling the user you are ready to use the skill, then ask the user for the details required by that skill. Do not assume the missing inputs. Let the skill guide the conversation and any files you create or update.
                """));
    }

    private static string SkillChoice(ProfileCatalogDocument skill)
    {
        return $"{skill.Name} ({skill.Slug})";
    }

    private static SupportedTerminalClient SelectAiClient(StartupContext context, IConsoleInteraction console)
    {
        IReadOnlyList<SupportedTerminalClient> clients = ProfileConfigurationReader
            .Read(context.ProfileDirectory)
            .SupportedClients
            .Where(IsSessionClientAvailable)
            .ToList();

        if (clients.Count == 0)
        {
            clients = SupportedTerminalClientCatalog
                .Create()
                .Where(IsSessionClientAvailable)
                .ToList();
        }

        if (clients.Count == 0)
        {
            throw new InvalidOperationException("No supported AI clients are installed or available on PATH.");
        }

        console.ShowMenuShell();
        string selected = console.Select("Which AI client should I use?", clients.Select(ClientChoice).ToList());

        return clients.First(client => selected.EndsWith($"({client.Slug})", StringComparison.Ordinal));
    }

    private static string ClientChoice(SupportedTerminalClient client)
    {
        return $"{client.Name} ({client.Slug})";
    }

    private static bool IsSessionClientAvailable(SupportedTerminalClient client)
    {
        return CommandExists(client.Session.BinaryPath);
    }

    private static string? LaunchClient(Action launch)
    {
        try
        {
            launch();

            return null;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return $"Could not start the selected AI client: {exception.Message}";
        }
    }
}
