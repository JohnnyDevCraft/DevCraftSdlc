using System.Text.Json;

namespace DevCraft.Cli;

public sealed class StartupFlow
{
    private readonly IConsoleInteraction console;
    private readonly IAiProjectScanner aiProjectScanner;
    private readonly Func<IReadOnlyList<TerminalAgent>> detectAgents;
    private readonly IDevCraftRepositoryUpgradeLauncher upgradeLauncher;

    public StartupFlow(
        IConsoleInteraction console,
        IAiProjectScanner aiProjectScanner,
        Func<IReadOnlyList<TerminalAgent>> detectAgents,
        IDevCraftRepositoryUpgradeLauncher? upgradeLauncher = null)
    {
        this.console = console;
        this.aiProjectScanner = aiProjectScanner;
        this.detectAgents = detectAgents;
        this.upgradeLauncher = upgradeLauncher ?? new TerminalDevCraftRepositoryUpgradeLauncher();
    }

    public bool Run(StartupContext context, bool forceInstall = false)
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

        if (ProfileHome.Is(context))
        {
            console.WriteStatus(ProfileHome.Notice(context));
            return true;
        }

        console.ShowStartupStage("Checking for code...");
        IReadOnlyList<MarkerDetection> markerDetections = MarkerScanner.Scan(context.CurrentDirectory);
        FolderReadinessResult readiness = FolderReadinessDetector.Detect(context.CurrentDirectory);
        console.ShowStartupStage("Checking for SDLC...");
        bool hasSdlcWorkflowMarkers = markerDetections.Any(detection => detection.Category == MarkerCategory.SdlcWorkflow);
        bool hasDevCraftMarkers = markerDetections.Any(detection => detection.IsDevCraft);

        if (hasDevCraftMarkers)
        {
            console.ShowStartupStage("DevCraft workflow detected.");
            if (forceInstall)
            {
                return UpgradeExistingDevCraft(context);
            }

            console.WriteStatus("Folder already uses DevCraft");
            return true;
        }

        if (forceInstall)
        {
            console.ShowStartupStage("Force installing DevCraft.");
            console.WriteStatus("Force installing DevCraft in the current folder");
            InstallDevCraft(context.CurrentDirectory, AskForNewProjectProfile(context.CurrentDirectory), context.ProfileDirectory);
            return true;
        }

        if (readiness.Readiness == FolderReadiness.ReadyForDevCraft && !hasSdlcWorkflowMarkers)
        {
            console.ShowStartupStage("No SDLC workflow detected.");
            console.WriteStatus("Folder ready for DevCraft");
            InstallDevCraft(context.CurrentDirectory, AskForNewProjectProfile(context.CurrentDirectory), context.ProfileDirectory);
            return true;
        }

        if (readiness.Readiness == FolderReadiness.ReadyForDevCraft)
        {
            console.ShowStartupStage("SDLC workflow detected.");
            console.WriteStatus("Folder has SDLC workflow markers");
            return true;
        }

        console.ShowStartupStage("Folder has code.");
        console.WriteStatus("Folder has code");

        string defaultAgent = ReadDefaultAgent(context.SoulFilePath);
        ProjectScanResult scanResult;

        try
        {
            scanResult = console.RunStatus(
                $"Scanning folder with {defaultAgent}...",
                () => aiProjectScanner.Scan(context.CurrentDirectory, defaultAgent, context.ProfileDirectory));
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            console.WriteStatus($"AI project scan failed: {exception.Message}");
            return false;
        }

        ConsoleProjectScanWriter.Write(scanResult);

        if (!hasSdlcWorkflowMarkers && !scanResult.AiDrivenSdlc.Detected)
        {
            ProjectProfile selectedProfile = SelectProjectProfile(context.CurrentDirectory, scanResult.ProjectProfile);
            InstallDevCraft(context.CurrentDirectory, selectedProfile, context.ProfileDirectory);
        }

        return true;
    }

    private bool UpgradeExistingDevCraft(StartupContext context)
    {
        ProjectDevCraftConfiguration projectConfiguration =
            ProjectDevCraftConfigurationReader.Read(context.CurrentDirectory)
            ?? CreateLegacyProjectConfiguration(context.CurrentDirectory);

        if (string.IsNullOrWhiteSpace(projectConfiguration.ProjectKey))
        {
            projectConfiguration = projectConfiguration with { ProjectKey = Guid.NewGuid().ToString() };
        }

        DevCraftProfileConfiguration profileConfiguration = ProfileConfigurationReader.Read(context.ProfileDirectory);
        string currentVersion = DevCraftCurrentVersionProvider.Get();
        string? repositoryVersion = DevCraftRepositoryVersionStore.Read(context.CurrentDirectory);
        DevCraftVersionComparison comparison = DevCraftVersionComparer.CompareRepositoryToCurrent(repositoryVersion, currentVersion);

        if (comparison == DevCraftVersionComparison.CurrentOrNewer)
        {
            console.WriteStatus($"Repository DevCraft version is current: {repositoryVersion}");
            return true;
        }

        string effectiveStorage = FeatureStorageResolver.Resolve(profileConfiguration, projectConfiguration);
        DevCraftRepositoryUpgradeRequest request = new(
            context,
            projectConfiguration,
            currentVersion,
            repositoryVersion,
            effectiveStorage);
        DevCraftRepositoryUpgradeResult result = upgradeLauncher.Upgrade(request);

        if (!result.Success)
        {
            console.WriteStatus($"DevCraft repository upgrade failed: {result.Summary}");
            return false;
        }

        RestoreProjectIdentity(context.CurrentDirectory, projectConfiguration);
        DevCraftRepositoryVersionStore.Write(context.CurrentDirectory, currentVersion);
        console.WriteStatus($"DevCraft repository upgraded to {currentVersion}: {result.Summary}");

        return true;
    }

    // The upgrade agent rewrites configure.json; put back a project key or profile it left out.
    private static void RestoreProjectIdentity(string repositoryDirectory, ProjectDevCraftConfiguration handedOff)
    {
        string path = ProjectDevCraftConfigurationStore.PathFor(repositoryDirectory);
        ProjectDevCraftConfiguration? written = null;
        bool hasKey = false;
        bool hasProfile = false;

        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement root = document.RootElement;
            hasKey = HasProperty(root, "ProjectKey", JsonValueKind.String, out JsonElement key) && key.GetString()!.Trim().Length > 0;
            hasProfile = HasProperty(root, "ProjectProfile", JsonValueKind.Object, out _);
            written = ProjectDevCraftConfigurationReader.Read(repositoryDirectory);
        }
        catch (Exception exception) when (exception is IOException or JsonException)
        {
        }

        if (written is not null && hasKey && hasProfile)
        {
            return;
        }

        ProjectDevCraftConfigurationStore.Write(
            repositoryDirectory,
            written is null
                ? handedOff
                : written with
                {
                    ProjectKey = hasKey ? written.ProjectKey : handedOff.ProjectKey,
                    ProjectProfile = hasProfile ? written.ProjectProfile : handedOff.ProjectProfile,
                });
    }

    private static bool HasProperty(JsonElement element, string name, JsonValueKind kind, out JsonElement value)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == kind)
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static ProjectDevCraftConfiguration CreateLegacyProjectConfiguration(string repositoryDirectory)
    {
        string projectName = Path.GetFileName(Path.GetFullPath(repositoryDirectory).TrimEnd(Path.DirectorySeparatorChar));
        ProjectProfile profile = new(
            projectName,
            "Legacy DevCraft project requiring configuration upgrade.",
            "Legacy DevCraft project requiring configuration upgrade.");

        return new ProjectDevCraftConfiguration(
            Guid.NewGuid().ToString(),
            null,
            "features.json",
            profile,
            []);
    }

    private ProjectProfile SelectProjectProfile(string directoryPath, ProjectProfile aiProfile)
    {
        string suggestedName = string.IsNullOrWhiteSpace(aiProfile.Name)
            ? DefaultProjectName(directoryPath)
            : aiProfile.Name.Trim();

        return EditProjectProfile(suggestedName, aiProfile.Description);
    }

    private ProjectProfile AskForNewProjectProfile(string directoryPath)
    {
        return EditProjectProfile(DefaultProjectName(directoryPath), string.Empty);
    }

    internal static string DefaultProjectName(string directoryPath)
    {
        return Path.GetFileName(Path.GetFullPath(directoryPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    // Cancel keeps the shown defaults: installation has no abort path, so Esc means "use what is shown".
    private ProjectProfile EditProjectProfile(string suggestedName, string suggestedDescription)
    {
        string projectName = suggestedName;

        while (true)
        {
            ValueEditResult nameResult = console.EditValue("Project Profile", "Project name", projectName, required: true);

            if (nameResult.Saved)
            {
                projectName = nameResult.Value.Trim();
            }

            if (!string.IsNullOrWhiteSpace(projectName))
            {
                break;
            }

            console.WriteStatus("Project name is required.");
            projectName = string.Empty;
        }

        TextEditResult descriptionResult = console.EditText(new TextEditOptions("Project Description", suggestedDescription));
        string projectDescription = descriptionResult.Saved ? descriptionResult.Text.Trim() : suggestedDescription.Trim();

        return new ProjectProfile(projectName, projectDescription, projectDescription);
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
            console.EditText(new TextEditOptions("Tell me what you do and how I'll be able to assist you.", string.Empty)).Text.Trim(),
            console.Ask("What would you like me to be called?"),
            console.EditText(new TextEditOptions("How would you like me to respond to you?", string.Empty)).Text.Trim(),
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

        return SoulDefaultAgentStore.Read(soulFilePath);
    }
}
