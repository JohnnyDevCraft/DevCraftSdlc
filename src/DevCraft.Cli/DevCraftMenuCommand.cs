namespace DevCraft.Cli;

public static class DevCraftMenuCommand
{
    public static Func<string, bool> CommandExists { get; set; } = CommandLocator.Exists;

    private const string ProjectManagement = "Project Management";
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
    private const string ConfigureSupportedClients = "Configure Supported Clients";
    private const string AddDevCraftToDesktopAgent = "Add DevCraft To Desktop Agent";
    private const string ManagePeople = "Manage People";
    private const string ManageLogs = "Manage Logs";
    private const string ListPeople = "List People";
    private const string ListLogEntries = "List Log Entries";
    private const string ListSummaries = "List Summaries";
    private const string AddPerson = "Add Person";
    private const string AddLogEntry = "Add Log Entry";
    private const string NewLogEntry = "New Log Entry";
    private const string Edit = "Edit";
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
                [ProjectManagement, Logging, SituationalConversation, Features, Skills, ConfigureDevCraft, Exit]);

            switch (selected)
            {
                case Exit:
                    return;
                case ProjectManagement:
                    notice = RunProjectManagementMenu(context, console, launcher, projectConfiguration);
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

    private static string? RunProjectManagementMenu(
        StartupContext context,
        IConsoleInteraction console,
        IDevCraftAiSessionLauncher launcher,
        ProjectDevCraftConfiguration projectConfiguration)
    {
        console.ShowMenuShell();
        string selected = console.Select(
            ProjectManagement,
            [ProjectDiscovery, ProjectDesign, ProjectTheme, ProjectSetup, CreateProject, Back]);

        return selected switch
        {
            Back => null,
            ProjectDiscovery => LaunchClient(
                () => launcher.Launch(
                    context,
                    projectConfiguration,
                    SelectAiClient(context, console),
                    "Work on project discovery. Use and maintain the repository DevCraft discovery artifact for this project.")),
            ProjectDesign => LaunchClient(
                () => launcher.Launch(
                    context,
                    projectConfiguration,
                    SelectAiClient(context, console),
                    "Work on project design and component design. Use and maintain the repository DevCraft design artifact for this project.")),
            ProjectTheme => LaunchClient(
                () => launcher.Launch(
                    context,
                    projectConfiguration,
                    SelectAiClient(context, console),
                    "Work on project theme. Use and maintain the repository DevCraft theme artifact for this project.")),
            ProjectSetup => LaunchClient(
                () => launcher.Launch(
                    context,
                    projectConfiguration,
                    SelectAiClient(context, console),
                    "Work on project setup. Use and maintain the repository DevCraft setup context for this project.")),
            CreateProject => RunCreateProject(context, console, launcher, projectConfiguration),
            _ => throw new InvalidOperationException($"Unsupported project management option: {selected}."),
        };
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
            [ImportSettings, ChangeFeatureStorage, ConfigureSituationalAwareness, ConfigureSupportedClients, AddDevCraftToDesktopAgent, CreateSkill, CreateStandards, CreateArchitecture, CreateProjectType, Back]);

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
            case ConfigureSupportedClients:
                RunConfigureSupportedClients(context, console);
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

        List<string> choices = [ManagePeople, ManageLogs];

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

        if (selected == ManagePeople)
        {
            return RunManagePeopleMenu(console, store);
        }

        if (selected == ManageLogs)
        {
            return RunManageLogsMenu(console, store);
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
        FieldEditResult fields = console.EditFields(new FieldEditOptions(
            "Person Details",
            [
                new FieldEditField("firstName", "First name", Required: true),
                new FieldEditField("lastName", "Last name"),
                new FieldEditField("email", "Email"),
                new FieldEditField("phone", "Phone"),
                new FieldEditField("jobTitle", "Job Title"),
                new FieldEditField("assignedTeam", "Assigned Team"),
                new FieldEditField("organization", "Organization"),
            ]));

        if (!fields.Saved)
        {
            console.WriteStatus("Person was not added.");
            return;
        }

        TextEditResult relation = console.EditText(new TextEditOptions("Relationship Details", string.Empty));

        if (!relation.Saved)
        {
            console.WriteStatus("Person was not added.");
            return;
        }

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
            FieldValue(fields, "firstName"),
            FieldValue(fields, "lastName"),
            FieldValue(fields, "email"),
            FieldValue(fields, "phone"),
            relation.Text.Trim(),
            status,
            FieldValue(fields, "jobTitle"),
            FieldValue(fields, "assignedTeam"),
            FieldValue(fields, "organization"),
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
                FieldEditResult fields = console.EditFields(new FieldEditOptions(
                    "Edit Name",
                    [
                        new FieldEditField("firstName", "First name", person.FirstName, Required: true),
                        new FieldEditField("lastName", "Last name", person.LastName),
                    ]));

                if (!fields.Saved)
                {
                    console.WriteStatus("Person was not changed.");
                    continue;
                }

                person = person with
                {
                    FirstName = FieldValue(fields, "firstName"),
                    LastName = FieldValue(fields, "lastName"),
                };
            }
            else if (selected == EditContact)
            {
                FieldEditResult fields = console.EditFields(new FieldEditOptions(
                    "Edit Contact",
                    [
                        new FieldEditField("email", "Email", person.Email),
                        new FieldEditField("phone", "Phone", person.Phone),
                    ]));

                if (!fields.Saved)
                {
                    console.WriteStatus("Person was not changed.");
                    continue;
                }

                person = person with
                {
                    Email = FieldValue(fields, "email"),
                    Phone = FieldValue(fields, "phone"),
                };
            }
            else if (selected == EditPosition)
            {
                FieldEditResult fields = console.EditFields(new FieldEditOptions(
                    "Edit Position",
                    [
                        new FieldEditField("jobTitle", "Job Title", person.JobTitle),
                        new FieldEditField("assignedTeam", "Assigned Team", person.AssignedTeam),
                        new FieldEditField("organization", "Organization", person.Organization),
                    ]));

                if (!fields.Saved)
                {
                    console.WriteStatus("Person was not changed.");
                    continue;
                }

                person = person with
                {
                    JobTitle = FieldValue(fields, "jobTitle"),
                    AssignedTeam = FieldValue(fields, "assignedTeam"),
                    Organization = FieldValue(fields, "organization"),
                };
            }
            else if (selected == EditRelationship)
            {
                TextEditResult relation = console.EditText(new TextEditOptions("Relationship Details", person.Relation));

                if (!relation.Saved)
                {
                    console.WriteStatus("Person was not changed.");
                    continue;
                }

                person = person with { Relation = relation.Text.Trim() };
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

    private static string? RunManageLogsMenu(IConsoleInteraction console, ISituationStore store)
    {
        while (true)
        {
            console.ShowMenuShell();
            string selected = console.Select("Manage Logs", [ListLogEntries, NewLogEntry, ListSummaries, Back]);

            if (selected == Back)
            {
                return null;
            }

            if (selected == NewLogEntry)
            {
                AddSituationLogEntry(console, store);
            }
            else if (selected == ListLogEntries)
            {
                RunLogEntryListMenu(console, store);
            }
            else if (selected == ListSummaries)
            {
                RunSummaryTypeMenu(console, store);
            }
        }
    }

    private static void AddSituationLogEntry(IConsoleInteraction console, ISituationStore store)
    {
        TextEditResult result = console.EditText(new TextEditOptions("New Log Entry", string.Empty));

        if (!result.Saved)
        {
            console.WriteStatus("Log entry was not added.");
            return;
        }

        if (string.IsNullOrWhiteSpace(result.Text))
        {
            console.WriteStatus("Empty log entries are ignored.");
            return;
        }

        store.AddLogEntry(new SituationLogEntry(Guid.NewGuid().ToString("N"), DateTimeOffset.Now, result.Text.Trim(), false));
        console.WriteStatus("Log entry added to situational awareness.");
    }

    private static void RunLogEntryListMenu(IConsoleInteraction console, ISituationStore store)
    {
        while (true)
        {
            IReadOnlyList<SituationLogEntry> entries = store
                .Read(true)
                .LogEntries
                .OrderByDescending(entry => entry.DateTime)
                .ToList();

            if (entries.Count == 0)
            {
                console.WriteStatus("No uncompressed log entries are tracked yet.");
                return;
            }

            List<string> choices = [GoBack];
            choices.AddRange(entries.Select(LogEntryChoice));
            console.ShowMenuShell();
            string selected = console.Select(ListLogEntries, choices);

            if (selected == GoBack)
            {
                return;
            }

            int selectedIndex = choices.IndexOf(selected) - 1;
            RunLogEntryEditMenu(console, store, entries[selectedIndex]);
        }
    }

    private static void RunLogEntryEditMenu(IConsoleInteraction console, ISituationStore store, SituationLogEntry entry)
    {
        while (true)
        {
            entry = store.Read(false).LogEntries.First(current => current.RowId.Equals(entry.RowId, StringComparison.OrdinalIgnoreCase));
            console.WriteStatus($"DateTime: {entry.DateTime:u}{Environment.NewLine}Content:{Environment.NewLine}{entry.LogData}");
            console.ShowMenuShell();
            string selected = console.Select(LogEntryChoice(entry), [Edit, GoBack]);

            if (selected == GoBack)
            {
                return;
            }

            DateTimeOffset? dateTime = AskDateTime(console, "Log entry date/time", entry.DateTime);
            TextEditResult content = console.EditText(new TextEditOptions("Edit Log Entry Content", entry.LogData));

            if (dateTime is null || !content.Saved)
            {
                console.WriteStatus("Log entry was not changed.");
                continue;
            }

            entry = entry with
            {
                DateTime = dateTime.Value,
                LogData = content.Text.Trim(),
            };
            store.UpsertLogEntry(entry);
            console.WriteStatus("Log entry updated.");
        }
    }

    private static void RunSummaryTypeMenu(IConsoleInteraction console, ISituationStore store)
    {
        while (true)
        {
            console.ShowMenuShell();
            string selected = console.Select("List Summaries", ["Week", "Month", "Sprint", "Quarter", "Year", GoBack]);

            if (selected == GoBack)
            {
                return;
            }

            RunSummaryListMenu(console, store, selected.ToLowerInvariant());
        }
    }

    private static void RunSummaryListMenu(IConsoleInteraction console, ISituationStore store, string type)
    {
        while (true)
        {
            IReadOnlyList<SituationSummary> summaries = store
                .Read(true)
                .Summaries
                .Where(summary => summary.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(summary => summary.DateTime)
                .ToList();

            if (summaries.Count == 0)
            {
                console.WriteStatus($"No uncompressed {type} summaries are tracked yet.");
                return;
            }

            List<string> choices = [GoBack];
            choices.AddRange(summaries.Select(SummaryChoice));
            console.ShowMenuShell();
            string selected = console.Select($"{type} summaries", choices);

            if (selected == GoBack)
            {
                return;
            }

            int selectedIndex = choices.IndexOf(selected) - 1;
            RunSummaryEditMenu(console, store, summaries[selectedIndex]);
        }
    }

    private static void RunSummaryEditMenu(IConsoleInteraction console, ISituationStore store, SituationSummary summary)
    {
        while (true)
        {
            summary = store.Read(false).Summaries.First(current => current.RowId.Equals(summary.RowId, StringComparison.OrdinalIgnoreCase));
            console.WriteStatus($"DateTime: {summary.DateTime:u}{Environment.NewLine}Type: {summary.Type}{Environment.NewLine}Content:{Environment.NewLine}{summary.SummaryData}");
            console.ShowMenuShell();
            string selected = console.Select(SummaryChoice(summary), [Edit, GoBack]);

            if (selected == GoBack)
            {
                return;
            }

            DateTimeOffset? dateTime = AskDateTime(console, "Summary date/time", summary.DateTime);
            TextEditResult content = console.EditText(new TextEditOptions("Edit Summary Content", summary.SummaryData));

            if (dateTime is null || !content.Saved)
            {
                console.WriteStatus("Summary was not changed.");
                continue;
            }

            summary = summary with
            {
                DateTime = dateTime.Value,
                SummaryData = content.Text.Trim(),
            };
            store.UpsertSummary(summary);
            console.WriteStatus("Summary updated.");
        }
    }

    private static DateTimeOffset? AskDateTime(IConsoleInteraction console, string prompt, DateTimeOffset current)
    {
        string value = console.Ask($"{prompt} [{current:u}]");

        if (string.IsNullOrWhiteSpace(value))
        {
            return current;
        }

        return DateTimeOffset.TryParse(value, out DateTimeOffset parsed)
            ? parsed
            : null;
    }

    private static string LogEntryChoice(SituationLogEntry entry)
    {
        return $"{entry.DateTime:yyyy-MM-dd HH:mm} | {SingleLinePreview(entry.LogData)}";
    }

    private static string SummaryChoice(SituationSummary summary)
    {
        return $"{summary.DateTime:yyyy-MM-dd HH:mm} | {SingleLinePreview(summary.SummaryData)}";
    }

    private static string SingleLinePreview(string value)
    {
        string preview = value.ReplaceLineEndings(" ").Trim();

        return preview.Length <= 80 ? preview : $"{preview[..77]}...";
    }

    private static void RunConfigureSupportedClients(StartupContext context, IConsoleInteraction console)
    {
        while (true)
        {
            DevCraftProfileConfiguration configuration = ProfileConfigurationReader.Read(context.ProfileDirectory);
            IReadOnlyList<SupportedTerminalClient> clients = configuration.SupportedClients;

            if (clients.Count == 0)
            {
                console.WriteStatus("There are no supported clients to configure.");
                return;
            }

            List<string> choices = clients.Select(ClientChoice).Append(Back).ToList();
            console.ShowMenuShell();
            string selected = console.Select("Configure Supported Clients", choices);

            if (selected == Back)
            {
                return;
            }

            SupportedTerminalClient client = clients.First(item => selected.EndsWith($"({item.Slug})", StringComparison.Ordinal));
            RunSupportedClientEditMenu(context, console, configuration, client);
        }
    }

    private static void RunSupportedClientEditMenu(
        StartupContext context,
        IConsoleInteraction console,
        DevCraftProfileConfiguration configuration,
        SupportedTerminalClient client)
    {
        console.ShowMenuShell();
        string selected = console.Select(ClientChoice(client), [Edit, Back]);

        if (selected == Back)
        {
            return;
        }

        FieldEditResult fields = console.EditFields(new FieldEditOptions(
            "Supported Client",
            [
                new FieldEditField("name", "Client name", client.Name, Required: true),
                new FieldEditField("slug", "Client slug", client.Slug, Required: true),
                new FieldEditField("scanBinary", "Scan binary path", client.Scan.BinaryPath, Required: true),
                new FieldEditField("sessionBinary", "Session binary path", client.Session.BinaryPath, Required: true),
            ]));
        TextEditResult description = console.EditText(new TextEditOptions("Client Description", client.Description));
        TextEditResult scanDescription = console.EditText(new TextEditOptions("Scan Operation Description", client.Scan.Description));
        TextEditResult scanArguments = console.EditText(new TextEditOptions("Scan Arguments", string.Join(Environment.NewLine, client.Scan.Arguments)));
        TextEditResult sessionDescription = console.EditText(new TextEditOptions("Session Operation Description", client.Session.Description));
        TextEditResult sessionArguments = console.EditText(new TextEditOptions("Session Arguments", string.Join(Environment.NewLine, client.Session.Arguments)));

        if (!fields.Saved || !description.Saved || !scanDescription.Saved || !scanArguments.Saved || !sessionDescription.Saved || !sessionArguments.Saved)
        {
            console.WriteStatus("Supported client was not changed.");
            return;
        }

        string name = FieldValue(fields, "name");
        string slug = FieldValue(fields, "slug");
        string scanBinary = FieldValue(fields, "scanBinary");
        string sessionBinary = FieldValue(fields, "sessionBinary");

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(scanBinary) || string.IsNullOrWhiteSpace(sessionBinary))
        {
            console.WriteStatus("Supported client was not changed. Name, slug, scan binary, and session binary are required.");
            return;
        }

        SupportedTerminalClient updated = new(
            slug.Trim(),
            name.Trim(),
            description.Text.Trim(),
            new TerminalClientOperation(
                scanDescription.Text.Trim(),
                scanBinary.Trim(),
                ParseArgumentLines(scanArguments.Text)),
            new TerminalClientOperation(
                sessionDescription.Text.Trim(),
                sessionBinary.Trim(),
                ParseArgumentLines(sessionArguments.Text)));
        List<SupportedTerminalClient> clients = configuration.SupportedClients.ToList();
        int index = clients.FindIndex(item => item.Slug.Equals(client.Slug, StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
        {
            clients[index] = updated;
        }
        else
        {
            clients.Add(updated);
        }

        DevCraftProfileConfigurationWriter.Write(context.ProfileDirectory, configuration with { SupportedClients = clients });
        console.WriteStatus("Supported client updated.");
    }

    private static IReadOnlyList<string> ParseArgumentLines(string value)
    {
        return value
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Select(argument => argument.Trim())
            .Where(argument => argument.Length > 0)
            .ToList();
    }

    private static string FieldValue(FieldEditResult result, string key)
    {
        return result.Values.TryGetValue(key, out string? value)
            ? value.Trim()
            : string.Empty;
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
