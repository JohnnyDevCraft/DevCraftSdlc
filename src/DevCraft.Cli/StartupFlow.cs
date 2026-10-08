namespace DevCraft.Cli;

public sealed class StartupFlow
{
    private readonly IConsoleInteraction console;
    private readonly IAiProjectScanner aiProjectScanner;
    private readonly Func<IReadOnlyList<TerminalAgent>> detectAgents;

    public StartupFlow(
        IConsoleInteraction console,
        IAiProjectScanner aiProjectScanner,
        Func<IReadOnlyList<TerminalAgent>> detectAgents)
    {
        this.console = console;
        this.aiProjectScanner = aiProjectScanner;
        this.detectAgents = detectAgents;
    }

    public void Run(StartupContext context)
    {
        console.ShowStartupStage("Scanning for profile...");
        ProfileState profileState = ProfileStateDetector.Detect(context);

        if (!profileState.ProfileDirectoryExists)
        {
            Directory.CreateDirectory(context.ProfileDirectory);
            console.WriteStatus($"Profile folder created: {context.ProfileDirectory}");
            profileState = ProfileStateDetector.Detect(context);
        }

        ProfileStructureResult profileStructure = ProfileStructureInitializer.Ensure(context.ProfileDirectory);

        foreach (string directory in profileStructure.CreatedDirectories)
        {
            console.WriteStatus($"Profile support folder created: {directory}");
        }

        foreach (string file in profileStructure.CreatedFiles)
        {
            console.WriteStatus($"Profile support file created: {file}");
        }

        console.ShowStartupStage("Scanning for soul...");

        if (!profileState.SoulFileExists)
        {
            RunSoulSetup(context);
        }
        else if (profileState.SoulFileExists)
        {
            console.WriteStatus($"Soul file found: {profileState.SoulFilePath}");
        }

        console.ShowStartupStage("Checking for code...");
        IReadOnlyList<MarkerDetection> markerDetections = MarkerScanner.Scan(context.CurrentDirectory);
        FolderReadinessResult readiness = FolderReadinessDetector.Detect(context.CurrentDirectory);
        console.ShowStartupStage("Checking for SDLC...");
        bool hasSdlcWorkflowMarkers = markerDetections.Any(detection => detection.Category == MarkerCategory.SdlcWorkflow);

        if (readiness.Readiness == FolderReadiness.ReadyForDevCraft && !hasSdlcWorkflowMarkers)
        {
            console.ShowStartupStage("No SDLC workflow detected.");
            console.WriteStatus("Folder ready for DevCraft");
            InstallDevCraft(context.CurrentDirectory, AskForNewProjectProfile(context.CurrentDirectory), context.ProfileDirectory);
            return;
        }

        if (readiness.Readiness == FolderReadiness.ReadyForDevCraft)
        {
            console.ShowStartupStage("SDLC workflow detected.");
            console.WriteStatus("Folder has SDLC workflow markers");
            return;
        }

        console.ShowStartupStage("Folder has code.");
        console.WriteStatus("Folder has code");

        string defaultAgent = ReadDefaultAgent(context.SoulFilePath);
        ProjectScanResult scanResult;

        try
        {
            scanResult = console.RunStatus(
                $"Scanning folder with {defaultAgent}...",
                () => aiProjectScanner.Scan(context.CurrentDirectory, defaultAgent));
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            console.WriteStatus($"AI project scan failed: {exception.Message}");
            return;
        }

        ConsoleProjectScanWriter.Write(scanResult);

        if (!hasSdlcWorkflowMarkers && !scanResult.AiDrivenSdlc.Detected)
        {
            ProjectProfile selectedProfile = SelectProjectProfile(scanResult.ProjectProfile);
            InstallDevCraft(context.CurrentDirectory, selectedProfile, context.ProfileDirectory);
        }
    }

    private ProjectProfile SelectProjectProfile(ProjectProfile aiProfile)
    {
        string selectedName = SelectSuggestedOrCustom(
            "What should we call this project?",
            aiProfile.Name,
            "Enter a custom project name");

        string selectedDescription = SelectSuggestedOrCustom(
            "Tell me what this project is about.",
            aiProfile.Description,
            "Enter a custom project description");

        return new ProjectProfile(selectedName, selectedDescription, selectedDescription);
    }

    private ProjectProfile AskForNewProjectProfile(string directoryPath)
    {
        string defaultName = Path.GetFileName(Path.GetFullPath(directoryPath).TrimEnd(Path.DirectorySeparatorChar));
        string projectName = console.Ask($"What should we call this project? [{defaultName}]");

        if (string.IsNullOrWhiteSpace(projectName))
        {
            projectName = defaultName;
        }

        string projectDescription = console.Ask("Tell me what this project is about.");

        return new ProjectProfile(projectName, projectDescription, projectDescription);
    }

    private string SelectSuggestedOrCustom(string prompt, string suggestion, string customChoice)
    {
        string useSuggestionChoice = $"Use AI suggestion: {suggestion}";
        string selected = console.Select(prompt, [useSuggestionChoice, customChoice]);

        if (selected == customChoice)
        {
            return console.Ask(prompt);
        }

        return suggestion;
    }

    private static void InstallDevCraft(string directoryPath, ProjectProfile? projectProfile, string profileDirectory)
    {
        DevCraftInstallationResult result = DevCraftInstaller.Install(directoryPath, projectProfile, profileDirectory);
        ConsoleDevCraftInstallationWriter.Write(result);
    }

    private void RunSoulSetup(StartupContext context)
    {
        IReadOnlyList<TerminalAgent> installedAgents = detectAgents()
            .Where(agent => agent.IsInstalled)
            .ToList();

        if (installedAgents.Count == 0)
        {
            installedAgents =
            [
                new TerminalAgent(TerminalAgentKind.Codex, "Codex", "codex", false),
                new TerminalAgent(TerminalAgentKind.Claude, "Claude AI", "claude", false),
                new TerminalAgent(TerminalAgentKind.GitHubCopilot, "GitHub Copilot", "gh copilot", false),
            ];
        }

        SoulSetupAnswers answers = new(
            console.Ask("What should I call you?"),
            console.Ask("Tell me what you do and how I'll be able to assist you."),
            console.Ask("What would you like me to be called?"),
            console.Ask("How would you like me to respond to you?"),
            console.Select("Which terminal AI agent should I use by default?", installedAgents.Select(agent => agent.DisplayName).ToList()));

        SoulFileWriter.Write(context.SoulFilePath, answers);
        console.WriteStatus($"Soul file created: {context.SoulFilePath}");
    }

    private static string ReadDefaultAgent(string soulFilePath)
    {
        if (!File.Exists(soulFilePath))
        {
            return "Codex";
        }

        string line = File
            .ReadLines(soulFilePath)
            .FirstOrDefault(value => value.StartsWith("- Default terminal AI agent:", StringComparison.OrdinalIgnoreCase))
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(line))
        {
            return "Codex";
        }

        return line.Split(':', 2)[1].Trim();
    }
}
