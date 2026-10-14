namespace DevCraft.Cli;

public static class FeatureCommand
{
    private const string Back = "Back";
    private const string FeatureActions = "Feature actions";
    private const string EditFeatureName = "Edit Feature Name";
    private const string OpenFeatureDirectory = "Open Feature Directory using Code";
    private const string StartConversation = "Start a conversation about the feature";

    public static void Run(
        StartupContext context,
        IReadOnlyList<string> args,
        IConsoleInteraction console,
        IFeatureAiSessionLauncher launcher,
        SupportedTerminalClient? client = null,
        Func<SupportedTerminalClient>? selectClient = null,
        IFeatureDirectoryOpener? directoryOpener = null)
    {
        ProfileStructureInitializer.Ensure(context.ProfileDirectory);
        ProjectDevCraftConfiguration? projectConfiguration = ProjectDevCraftConfigurationReader.Read(context.CurrentDirectory);

        if (projectConfiguration is null)
        {
            console.WriteStatus("DevCraft is not initialized in this repository.");
            return;
        }

        SystemCentralProjectsSynchronizer.Sync(context, projectConfiguration);

        if (args.Count == 0)
        {
            console.WriteStatus("Use feature list or feature new <name>.");
            return;
        }

        if (args[0].Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            DevCraftProfileConfiguration profileConfiguration = ProfileConfigurationReader.Read(context.ProfileDirectory);
            ListFeatures(context, projectConfiguration, profileConfiguration, console, launcher, client, selectClient, directoryOpener ?? new CodeFeatureDirectoryOpener());
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

            DevCraftProfileConfiguration profileConfiguration = ProfileConfigurationReader.Read(context.ProfileDirectory);
            CreateFeature(context, projectConfiguration, profileConfiguration, featureName, console, launcher, client ?? SelectFallbackClient(context));
            return;
        }

        console.WriteStatus("Use feature list or feature new <name>.");
    }

    private static void ListFeatures(
        StartupContext context,
        ProjectDevCraftConfiguration projectConfiguration,
        DevCraftProfileConfiguration profileConfiguration,
        IConsoleInteraction console,
        IFeatureAiSessionLauncher launcher,
        SupportedTerminalClient? client,
        Func<SupportedTerminalClient>? selectClient,
        IFeatureDirectoryOpener directoryOpener)
    {
        IReadOnlyList<DevCraftFeature> features = projectConfiguration.Features;

        if (features.Count == 0)
        {
            console.WriteStatus("No features found.");
            return;
        }

        string selected = console.Select(
            "Select a feature",
            features.Select(feature => $"{feature.Name} ({feature.Slug})").Append(Back).ToList());

        if (selected == Back)
        {
            return;
        }

        DevCraftFeature feature = features.First(item => selected.EndsWith($"({item.Slug})", StringComparison.Ordinal));
        console.WriteStatus($"Selected feature: {feature.Name}");

        string? notice = null;

        while (true)
        {
            string action = console.Select(
                notice is null
                    ? FeatureActions
                    : $"{notice}{Environment.NewLine}{Environment.NewLine}{FeatureActions}",
                [EditFeatureName, OpenFeatureDirectory, StartConversation, Back]);

            if (action == Back)
            {
                return;
            }

            if (action == EditFeatureName)
            {
                (projectConfiguration, feature, notice) = RenameFeature(context, projectConfiguration, feature, console);
                console.WriteStatus(notice);
                continue;
            }

            if (action == OpenFeatureDirectory)
            {
                notice = OpenDirectory(context, feature, directoryOpener);
                console.WriteStatus(notice);
                continue;
            }

            SupportedTerminalClient selectedClient = client ?? selectClient?.Invoke() ?? SelectFallbackClient(context);
            launcher.Launch(
                context,
                FeatureStorageResolver.WithEffectiveStorage(profileConfiguration, projectConfiguration),
                feature,
                selectedClient,
                "Work on existing feature.");
            return;
        }
    }

    private static (ProjectDevCraftConfiguration Configuration, DevCraftFeature Feature, string Notice) RenameFeature(
        StartupContext context,
        ProjectDevCraftConfiguration projectConfiguration,
        DevCraftFeature feature,
        IConsoleInteraction console)
    {
        ValueEditResult edit = console.EditValue("Edit Feature Name", "Feature name", feature.Name, required: true);

        if (!edit.Saved)
        {
            return (projectConfiguration, feature, "Feature name is unchanged.");
        }

        string newName = edit.Value.Trim();

        if (string.IsNullOrWhiteSpace(newName))
        {
            return (projectConfiguration, feature, "Feature name is required.");
        }

        if (newName.Equals(feature.Name, StringComparison.Ordinal))
        {
            return (projectConfiguration, feature, "Feature name is unchanged.");
        }

        DevCraftFeature renamedFeature = feature with
        {
            Name = newName,
        };
        ProjectDevCraftConfiguration updatedConfiguration = projectConfiguration with
        {
            Features = projectConfiguration.Features
                .Select(item => ReferenceEquals(item, feature) ? renamedFeature : item)
                .ToList(),
        };

        ProjectDevCraftConfigurationStore.Write(context.CurrentDirectory, updatedConfiguration);
        SystemCentralProjectsSynchronizer.Sync(context, updatedConfiguration);

        return (updatedConfiguration, renamedFeature, $"Feature renamed: {feature.Name} -> {newName}");
    }

    private static string OpenDirectory(
        StartupContext context,
        DevCraftFeature feature,
        IFeatureDirectoryOpener directoryOpener)
    {
        string? directory = FeatureDirectory(context, feature);

        if (directory is null)
        {
            return $"{feature.Name} uses {feature.StorageType} storage and has no local feature directory.";
        }

        if (!Directory.Exists(directory))
        {
            return $"Feature directory not found: {directory}";
        }

        return directoryOpener.Open(directory)
            ? $"Opened feature directory in Visual Studio Code: {directory}"
            : "Could not start Visual Studio Code. Make sure the `code` command is installed and on your PATH.";
    }

    private static string? FeatureDirectory(StartupContext context, DevCraftFeature feature)
    {
        if (feature.StorageType.Equals(FeatureStorageMode.SystemCentral, StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(context.ProfileDirectory, "features", feature.FolderName);
        }

        if (feature.StorageType.Equals(FeatureStorageMode.RepoCentral, StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(context.CurrentDirectory, ".devcraft", "features", feature.FolderName);
        }

        return null;
    }

    private static void CreateFeature(
        StartupContext context,
        ProjectDevCraftConfiguration projectConfiguration,
        DevCraftProfileConfiguration profileConfiguration,
        string featureName,
        IConsoleInteraction console,
        IFeatureAiSessionLauncher launcher,
        SupportedTerminalClient client)
    {
        string storageType = FeatureStorageResolver.Resolve(profileConfiguration, projectConfiguration);
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
        SystemCentralProjectsSynchronizer.Sync(context, updatedConfiguration);

        if (FeatureDirectory(context, feature) is string featureDirectory)
        {
            Directory.CreateDirectory(featureDirectory);
        }

        console.WriteStatus($"Feature created: {feature.Name}");
        launcher.Launch(
            context,
            FeatureStorageResolver.WithEffectiveStorage(profileConfiguration, updatedConfiguration),
            feature,
            client,
            "Create a new feature.");
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
