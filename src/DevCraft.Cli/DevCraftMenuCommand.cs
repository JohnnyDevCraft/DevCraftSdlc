namespace DevCraft.Cli;

public static class DevCraftMenuCommand
{
    public static Func<string, bool> CommandExists { get; set; } = CommandLocator.Exists;

    private const string ProjectDiscovery = "Project discovery";
    private const string ProjectDesign = "Project design";
    private const string ProjectTheme = "Project theme";
    private const string ProjectSetup = "Project setup";
    private const string CreateProject = "Create Project";
    private const string Logging = "Logging";
    private const string SituationalConversation = "Situational Conversation";
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
    private const string ConfigureSituationalAwareness = "Configure Situational Awareness";
    private const string AddDevCraftToDesktopAgent = "Add DevCraft To Desktop Agent";
    private const string ManagePeople = "Manage People";
    private const string ListPeople = "List People";
    private const string AddPerson = "Add Person";
    private const string AddLogEntry = "Add Log Entry";
    private const string GoBack = "Go Back";
    private const string EditName = "Edit Name";
    private const string EditContact = "Edit Contact";
    private const string EditPosition = "Edit Position";
    private const string EditRelationship = "Edit Relationship";
    private const string MakeInactive = "Make Inactive";
    private const string MakeActive = "Make Active";
    private const string CompressWeek = "Compress Week";
    private const string CompressSprint = "Compress Sprint";
    private const string CompressMonth = "Compress Month";
    private const string CompressQuarter = "Compress Quarter";
    private const string CompressYear = "Compress Year";

    public static void Run(
        StartupContext context,
        IConsoleInteraction console,
        IDevCraftAiSessionLauncher launcher,
        IFeatureAiSessionLauncher featureLauncher,
        ISituationSummaryGenerator? summaryGenerator = null)
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
                [ProjectDiscovery, ProjectDesign, ProjectTheme, ProjectSetup, CreateProject, Logging, SituationalConversation, Features, Skills, ConfigureDevCraft, Exit]);

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
                case Logging:
                    notice = RunLoggingMenu(context, console, summaryGenerator ?? new TerminalSituationSummaryGenerator());
                    break;
                case SituationalConversation:
                    notice = RunSituationalConversation(context, console, launcher, projectConfiguration);
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

    private static string? RunSituationalConversation(
        StartupContext context,
        IConsoleInteraction console,
        IDevCraftAiSessionLauncher launcher,
        ProjectDevCraftConfiguration projectConfiguration)
    {
        DevCraftProfileConfiguration configuration = ProfileConfigurationReader.Read(context.ProfileDirectory);

        if (!configuration.SituationEnabled)
        {
            return "Situational awareness is currently disabled. Use Configure DevCraft > Configure Situational Awareness to enable it.";
        }

        return LaunchClient(
            () => launcher.Launch(
                context,
                projectConfiguration,
                SelectAiClient(context, console),
                $"""
                Start a situational conversation with the user.

                This is not a repository implementation or feature workflow. Use it to help the user plan the day, remember past context that is available through situational awareness, and turn that context into a practical next-step conversation.

                First, ask the user what they want to accomplish today. Use the situational-awareness guidance in the DevCraft handoff to read relevant people, log entries, and summaries directly when useful. Do not claim that situational awareness is loaded unless the records are actually available to you through the provided file or database access.

                For conversational project and feature tracking, read the profile projects index at {Path.Combine(context.ProfileDirectory, "features", "projects.json")} when it is relevant. It tracks projects by id, name, repo-location, repo-name, and features with id, feature-name, description, work-item-id, and status. The canonical DevCraft feature statuses are Discovery, Clarification, Research, Planning, Analysis, Implementation, and Complete. Update tracked project and feature information only when the conversation establishes a real change; do not manufacture projects, features, work item IDs, or status changes just to fill the index. Setting a status records the actual DevCraft state only and does not authorize implementation or bypass DevCraft gates.
                """));
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
            [ImportSettings, ChangeFeatureStorage, ConfigureSituationalAwareness, AddDevCraftToDesktopAgent, CreateSkill, CreateStandards, CreateArchitecture, CreateProjectType, Back]);

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
            case ConfigureSituationalAwareness:
                RunConfigureSituationalAwareness(context, console);
                break;
            case AddDevCraftToDesktopAgent:
                ShowDesktopAgentInstructions(context, console);
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

    private static string? RunLoggingMenu(
        StartupContext context,
        IConsoleInteraction console,
        ISituationSummaryGenerator summaryGenerator)
    {
        DevCraftProfileConfiguration configuration = ProfileConfigurationReader.Read(context.ProfileDirectory);

        if (!configuration.SituationEnabled)
        {
            return "Situational awareness is currently disabled. Use Configure DevCraft > Configure Situational Awareness to enable it.";
        }

        ISituationStore store;

        try
        {
            store = SituationStoreFactory.Create(context.ProfileDirectory, configuration);
        }
        catch (InvalidOperationException exception)
        {
            return exception.Message;
        }

        List<string> choices = [ManagePeople, AddLogEntry];

        if (configuration.SituationScale == SituationScale.Weeks)
        {
            choices.Add(CompressWeek);
            choices.Add(CompressMonth);
        }
        else
        {
            choices.Add(CompressSprint);
        }

        choices.Add(CompressQuarter);
        choices.Add(CompressYear);
        choices.Add(Back);

        console.ShowMenuShell();
        string selected = console.Select("Logging", choices);

        if (selected == Back)
        {
            return null;
        }

        if (selected == AddLogEntry)
        {
            string logData = console.Ask("Log entry");

            if (string.IsNullOrWhiteSpace(logData))
            {
                return "Empty log entries are ignored.";
            }

            store.AddLogEntry(new SituationLogEntry(Guid.NewGuid().ToString("N"), DateTimeOffset.Now, logData.Trim(), false));

            return "Log entry added to situational awareness.";
        }

        if (selected == ManagePeople)
        {
            return RunManagePeopleMenu(console, store);
        }

        string type = selected switch
        {
            CompressWeek => "week",
            CompressSprint => "sprint",
            CompressMonth => "month",
            CompressQuarter => "quarter",
            CompressYear => "year",
            _ => throw new InvalidOperationException($"Unsupported logging option: {selected}."),
        };

        try
        {
            return new SituationCompressionService(store, summaryGenerator)
                .Compress(context, SelectAiClient(context, console), configuration, type);
        }
        catch (InvalidOperationException exception)
        {
            return exception.Message;
        }
    }

    private static string? RunManagePeopleMenu(IConsoleInteraction console, ISituationStore store)
    {
        while (true)
        {
            console.ShowMenuShell();
            string selected = console.Select("Manage People", [ListPeople, AddPerson, Back]);

            if (selected == Back)
            {
                return null;
            }

            if (selected == AddPerson)
            {
                AddSituationPerson(console, store);
                continue;
            }

            if (selected == ListPeople)
            {
                RunPeopleListMenu(console, store);
            }
        }
    }

    private static void AddSituationPerson(IConsoleInteraction console, ISituationStore store)
    {
        string firstName = console.Ask("First name");
        string lastName = console.Ask("Last name");
        string email = console.Ask("Email");
        string phone = console.Ask("Phone");
        string jobTitle = console.Ask("Job Title");
        string assignedTeam = console.Ask("Assigned Team");
        string organization = console.Ask("Organization");
        string relation = console.Ask("Relationship details");
        string status = console.Select("Status", ["ACTIVE", "INACTIVE"]);
        DateTimeOffset? inactiveDate = null;

        if (status == "INACTIVE")
        {
            inactiveDate = AskInactiveDate(console);

            if (inactiveDate is null)
            {
                console.WriteStatus("Invalid inactive date. Person was not added.");
                return;
            }
        }

        store.AddPerson(new SituationPerson(
            Guid.NewGuid().ToString("N"),
            firstName,
            lastName,
            email,
            phone,
            relation,
            status,
            jobTitle,
            assignedTeam,
            organization,
            inactiveDate));
        console.WriteStatus("Person added to situational awareness.");
    }

    private static void RunPeopleListMenu(IConsoleInteraction console, ISituationStore store)
    {
        while (true)
        {
            IReadOnlyList<SituationPerson> people = store
                .Read(false)
                .People
                .OrderBy(person => person.FirstName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(person => person.LastName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (people.Count == 0)
            {
                console.WriteStatus("No people are tracked yet.");
                return;
            }

            List<string> menuChoices = [GoBack];
            menuChoices.AddRange(people.Select(PersonChoice));

            console.ShowMenuShell();
            string selected = console.Select(ListPeople, menuChoices);

            if (selected == GoBack)
            {
                return;
            }

            int selectedIndex = menuChoices.IndexOf(selected) - 1;
            RunPersonEditMenu(console, store, people[selectedIndex]);
        }
    }

    private static void RunPersonEditMenu(IConsoleInteraction console, ISituationStore store, SituationPerson person)
    {
        while (true)
        {
            person = store.Read(false).People.First(current => current.RowId.Equals(person.RowId, StringComparison.OrdinalIgnoreCase));
            console.WriteStatus(PersonDetails(person));
            bool inactive = person.Status.Equals("INACTIVE", StringComparison.OrdinalIgnoreCase);
            string statusAction = inactive ? MakeActive : MakeInactive;

            console.ShowMenuShell();
            string selected = console.Select(PersonChoice(person), [EditName, EditContact, EditPosition, EditRelationship, statusAction, GoBack]);

            if (selected == GoBack)
            {
                return;
            }

            if (selected == EditName)
            {
                person = person with
                {
                    FirstName = console.Ask("First name"),
                    LastName = console.Ask("Last name"),
                };
            }
            else if (selected == EditContact)
            {
                person = person with
                {
                    Email = console.Ask("Email"),
                    Phone = console.Ask("Phone"),
                };
            }
            else if (selected == EditPosition)
            {
                person = person with
                {
                    JobTitle = console.Ask("Job Title"),
                    AssignedTeam = console.Ask("Assigned Team"),
                    Organization = console.Ask("Organization"),
                };
            }
            else if (selected == EditRelationship)
            {
                person = person with
                {
                    Relation = console.Ask("Relationship details"),
                };
            }
            else if (selected == MakeInactive)
            {
                DateTimeOffset? inactiveDate = AskInactiveDate(console);

                if (inactiveDate is null)
                {
                    console.WriteStatus("Invalid inactive date. Person status was not changed.");
                    continue;
                }

                person = person with
                {
                    Status = "INACTIVE",
                    InactiveDate = inactiveDate,
                };
            }
            else if (selected == MakeActive)
            {
                person = person with
                {
                    Status = "ACTIVE",
                    InactiveDate = null,
                };
            }

            store.UpsertPerson(person);
            console.WriteStatus("Person updated.");
        }
    }

    private static DateTimeOffset? AskInactiveDate(IConsoleInteraction console)
    {
        string value = console.Ask("Inactive date");

        return DateTime.TryParse(value, out DateTime date)
            ? new DateTimeOffset(DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified), TimeSpan.Zero)
            : null;
    }

    private static string PersonChoice(SituationPerson person)
    {
        return $"{person.FirstName} {person.LastName} ({person.Email}) | {person.JobTitle} | {person.AssignedTeam} | {person.Organization}";
    }

    private static string PersonDetails(SituationPerson person)
    {
        return $"""
            Name: {person.FirstName} {person.LastName}
            Contact: {person.Email} | {person.Phone}
            Position: {person.JobTitle} | {person.AssignedTeam} | {person.Organization}
            Relationship: {person.Relation}
            Status: {PersonStatusDisplay(person)}
            """;
    }

    private static string PersonStatusDisplay(SituationPerson person)
    {
        if (person.Status.Equals("INACTIVE", StringComparison.OrdinalIgnoreCase) && person.InactiveDate is not null)
        {
            return $"INACTIVE ({person.InactiveDate.Value.Date:M/d/yyyy})";
        }

        return person.Status;
    }

    private static void RunConfigureSituationalAwareness(StartupContext context, IConsoleInteraction console)
    {
        DevCraftProfileConfiguration oldConfiguration = ProfileConfigurationReader.Read(context.ProfileDirectory);
        bool enabled = console.Select("Enable Situational Awareness", ["No", "Yes"]) == "Yes";
        string scale = oldConfiguration.SituationScale;
        string storage = oldConfiguration.SituationStorage;
        string? connection = oldConfiguration.SituationConnection;

        if (enabled)
        {
            scale = console.Select("Use Sprints or Months & Weeks", ["Months & Weeks", "Sprints"]) == "Sprints"
                ? SituationScale.Sprint
                : SituationScale.Weeks;
            storage = console.Select("Use File or Database", ["File", "Database"]) == "Database"
                ? SituationStorage.Database
                : SituationStorage.File;

            if (storage == SituationStorage.Database)
            {
                console.WriteStatus(MongoDockerInstructions.Text);
                connection = console.Ask("Enter Database Connection String");
            }
            else
            {
                connection = null;
            }
        }

        DevCraftProfileConfiguration newConfiguration = oldConfiguration with
        {
            SituationEnabled = enabled,
            SituationScale = SituationScale.Normalize(scale),
            SituationStorage = SituationStorage.Normalize(storage),
            SituationConnection = string.IsNullOrWhiteSpace(connection) ? null : connection.Trim(),
        };

        SituationMigrationService.Migrate(context.ProfileDirectory, oldConfiguration, newConfiguration);
        DevCraftProfileConfigurationWriter.Write(context.ProfileDirectory, newConfiguration);
        console.WriteStatus("Situational awareness configuration updated.");
    }

    private static void ShowDesktopAgentInstructions(StartupContext context, IConsoleInteraction console)
    {
        string path = Path.Combine(context.ProfileDirectory, "desktop-agent-instructions.txt");

        if (!File.Exists(path))
        {
            ProfileStructureInitializer.Ensure(context.ProfileDirectory);
        }

        console.WriteStatus("Copy and paste this text into your desktop agent's instructions:");
        console.WriteStatus(File.ReadAllText(path));
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
