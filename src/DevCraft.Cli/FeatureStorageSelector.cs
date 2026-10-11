namespace DevCraft.Cli;

public static class FeatureStorageSelector
{
    private const string Back = "Back";
    private const string UseProfileDefault = "Use Profile Default";

    public static ProjectDevCraftConfiguration EnsureSelected(
        StartupContext context,
        IConsoleInteraction console,
        ProjectDevCraftConfiguration projectConfiguration)
    {
        DevCraftProfileConfiguration profileConfiguration = ProfileConfigurationReader.Read(context.ProfileDirectory);

        return FeatureStorageResolver.WithEffectiveStorage(profileConfiguration, projectConfiguration);
    }

    public static ProjectDevCraftConfiguration? Change(
        StartupContext context,
        IConsoleInteraction console,
        ProjectDevCraftConfiguration projectConfiguration)
    {
        return SelectAndSave(
            context,
            console,
            projectConfiguration,
            "Select the feature storage mechanism for this repository",
            includeBack: true);
    }

    private static ProjectDevCraftConfiguration? SelectAndSave(
        StartupContext context,
        IConsoleInteraction console,
        ProjectDevCraftConfiguration projectConfiguration,
        string prompt,
        bool includeBack)
    {
        IReadOnlyList<ProfileCatalogDocument> storageTypes = ProfileConfigurationReader
            .Read(context.ProfileDirectory)
            .FeatureStorageTypes;
        List<string> choices = storageTypes.Count == 0
            ? [FeatureStorageMode.RepoCentral, FeatureStorageMode.SystemCentral]
            : storageTypes.Select(StorageChoice).ToList();

        choices.Insert(0, UseProfileDefault);

        if (includeBack)
        {
            choices.Add(Back);
        }

        console.ShowMenuShell();
        string selected = console.Select(prompt, choices);

        if (selected == Back)
        {
            return null;
        }

        if (selected == UseProfileDefault)
        {
            ProjectDevCraftConfiguration defaultConfiguration = projectConfiguration with
            {
                SelectedFeatureStorage = null,
            };

            ProjectDevCraftConfigurationStore.Write(context.CurrentDirectory, defaultConfiguration);
            console.WriteStatus("Repository feature storage override cleared. This repository will use the profile default.");

            return defaultConfiguration;
        }

        string slug = SelectedSlug(selected, storageTypes);
        ProjectDevCraftConfiguration updatedConfiguration = projectConfiguration with
        {
            SelectedFeatureStorage = slug,
        };

        ProjectDevCraftConfigurationStore.Write(context.CurrentDirectory, updatedConfiguration);
        console.WriteStatus($"Feature storage set for this repository: {slug}");

        return updatedConfiguration;
    }

    private static string StorageChoice(ProfileCatalogDocument storageType)
    {
        return $"{ToTitleCase(storageType.Name)} ({storageType.Slug})";
    }

    private static string SelectedSlug(string selected, IReadOnlyList<ProfileCatalogDocument> storageTypes)
    {
        ProfileCatalogDocument? matchedStorageType = storageTypes
            .FirstOrDefault(item => selected.EndsWith($"({item.Slug})", StringComparison.Ordinal));

        if (matchedStorageType is not null)
        {
            return matchedStorageType.Slug;
        }

        int open = selected.LastIndexOf('(');
        int close = selected.LastIndexOf(')');
        if (open >= 0 && close > open)
        {
            return selected[(open + 1)..close];
        }

        return selected;
    }

    private static string ToTitleCase(string value)
    {
        return string.Join(
            ' ',
            value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
    }
}
