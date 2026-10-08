namespace DevCraft.Cli;

public static class FeatureCommand
{
    public static void Run(
        StartupContext context,
        IReadOnlyList<string> args,
        IConsoleInteraction console,
        IFeatureAiSessionLauncher launcher,
        SupportedTerminalClient? client = null)
    {
        ProfileStructureInitializer.Ensure(context.ProfileDirectory);
        ProjectDevCraftConfiguration? projectConfiguration = ProjectDevCraftConfigurationReader.Read(context.CurrentDirectory);

        if (projectConfiguration is null)
        {
            console.WriteStatus("DevCraft is not initialized in this repository.");
            return;
        }

        if (args.Count == 0)
        {
            console.WriteStatus("Use feature list or feature new <name>.");
            return;
        }

        if (args[0].Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            projectConfiguration = FeatureStorageSelector.EnsureSelected(context, console, projectConfiguration);
            ListFeatures(context, projectConfiguration, console, launcher, client ?? SelectFallbackClient(context));
            return;
        }

        if (args[0].Equals("new", StringComparison.OrdinalIgnoreCase))
        {
            string featureName = string.Join(' ', args.Skip(1)).Trim();

            if (string.IsNullOrWhiteSpace(featureName))
            {
                console.WriteStatus("Feature name is required.");
                return;
            }

            projectConfiguration = FeatureStorageSelector.EnsureSelected(context, console, projectConfiguration);
            CreateFeature(context, projectConfiguration, featureName, console, launcher, client ?? SelectFallbackClient(context));
            return;
        }

        console.WriteStatus("Use feature list or feature new <name>.");
    }

    private static void ListFeatures(
        StartupContext context,
        ProjectDevCraftConfiguration projectConfiguration,
        IConsoleInteraction console,
        IFeatureAiSessionLauncher launcher,
        SupportedTerminalClient client)
    {
        IReadOnlyList<DevCraftFeature> features = projectConfiguration.Features;

        if (features.Count == 0)
        {
            console.WriteStatus("No features found.");
            return;
        }

        string selected = console.Select(
            "Select a feature",
            features.Select(feature => $"{feature.Name} ({feature.Slug})").ToList());
        DevCraftFeature feature = features.First(item => selected.EndsWith($"({item.Slug})", StringComparison.Ordinal));
        console.WriteStatus($"Selected feature: {feature.Name}");
        launcher.Launch(context, projectConfiguration, feature, client, "Work on existing feature.");
    }

    private static void CreateFeature(
        StartupContext context,
        ProjectDevCraftConfiguration projectConfiguration,
        string featureName,
        IConsoleInteraction console,
        IFeatureAiSessionLauncher launcher,
        SupportedTerminalClient client)
    {
        string storageType = projectConfiguration.SelectedFeatureStorage
            ?? throw new InvalidOperationException("Feature storage must be selected before creating a feature.");
        string slug = UniqueSlug(SlugGenerator.Create(featureName), projectConfiguration.Features);
        DevCraftFeature feature = new(
            featureName,
            slug,
            "Pending.",
            Guid.NewGuid().ToString(),
            storageType,
            null);
        ProjectDevCraftConfiguration updatedConfiguration = projectConfiguration with
        {
            Features = projectConfiguration.Features.Concat([feature]).ToList(),
        };

        ProjectDevCraftConfigurationStore.Write(context.CurrentDirectory, updatedConfiguration);

        if (storageType.Equals(FeatureStorageMode.SystemCentral, StringComparison.OrdinalIgnoreCase))
        {
            AddSystemCentralFeature(context, updatedConfiguration, feature);
        }
        else if (storageType.Equals(FeatureStorageMode.RepoCentral, StringComparison.OrdinalIgnoreCase))
        {
            Directory.CreateDirectory(Path.Combine(context.CurrentDirectory, ".devcraft", "features", feature.FolderName));
        }

        console.WriteStatus($"Feature created: {feature.Name}");
        launcher.Launch(context, updatedConfiguration, feature, client, "Create a new feature.");
    }

    private static void AddSystemCentralFeature(
        StartupContext context,
        ProjectDevCraftConfiguration projectConfiguration,
        DevCraftFeature feature)
    {
        SystemCentralProjectsIndex index = SystemCentralProjectsStore.Read(context.ProfileDirectory);
        List<SystemCentralProject> projects = index.Projects.ToList();
        int projectIndex = projects.FindIndex(project => project.ProjectKey.Equals(projectConfiguration.ProjectKey, StringComparison.OrdinalIgnoreCase));
        SystemCentralProject project = projectIndex >= 0
            ? projects[projectIndex]
            : new SystemCentralProject(
                projectConfiguration.ProjectKey,
                projectConfiguration.ProjectProfile.Name,
                SlugGenerator.Create(projectConfiguration.ProjectProfile.Name),
                context.CurrentDirectory,
                []);
        List<DevCraftFeature> features = project.Features.ToList();

        if (!features.Any(item => item.Slug.Equals(feature.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            features.Add(feature);
        }

        project = project with
        {
            RepositoryPath = context.CurrentDirectory,
            Features = features,
        };

        if (projectIndex >= 0)
        {
            projects[projectIndex] = project;
        }
        else
        {
            projects.Add(project);
        }

        SystemCentralProjectsStore.Write(context.ProfileDirectory, new SystemCentralProjectsIndex(projects));
        Directory.CreateDirectory(Path.Combine(context.ProfileDirectory, "features", feature.FolderName));
    }

    private static string UniqueSlug(string slug, IReadOnlyList<DevCraftFeature> features)
    {
        string candidate = slug;
        int suffix = 2;

        while (features.Any(feature => feature.Slug.Equals(candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{slug}-{suffix}";
            suffix++;
        }

        return candidate;
    }

    private static SupportedTerminalClient SelectFallbackClient(StartupContext context)
    {
        IReadOnlyList<SupportedTerminalClient> clients = ProfileConfigurationReader.Read(context.ProfileDirectory).SupportedClients;

        return clients.Count > 0
            ? clients[0]
            : SupportedTerminalClientCatalog.Create()[0];
    }
}
