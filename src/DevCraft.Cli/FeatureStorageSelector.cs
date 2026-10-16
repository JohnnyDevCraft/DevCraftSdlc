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
        ProjectDevCraftConfiguration projectConfiguration,
        IGitRunner? gitRunner = null)
    {
        ProjectDevCraftConfiguration? updated = SelectAndSave(
            context,
            console,
            projectConfiguration,
            "Select the feature storage mechanism for this repository",
            includeBack: true);

        if (updated is not null)
        {
            OfferMoves(context, console, updated, gitRunner ?? new ProcessGitRunner());
        }

        return updated;
    }

    // Existing features keep their storage unless the operator picks them. Only the two local storage types
    // can be moved here; moving to ADO, GitHub, or JIRA comes in a later feature.
    private static void OfferMoves(
        StartupContext context,
        IConsoleInteraction console,
        ProjectDevCraftConfiguration configuration,
        IGitRunner git)
    {
        string target = FeatureStorageResolver.Resolve(ProfileConfigurationReader.Read(context.ProfileDirectory), configuration);

        if (!FeatureStorageMode.IsLocal(target))
        {
            console.WriteStatus(
                $"Moving existing features to {target} will come in a later feature. Existing features stay where they are; new features will use {target}.");
            return;
        }

        FeatureIndex index;

        try
        {
            index = FeatureIndex.Load(context, configuration, git, console);
        }
        catch (ProjectsIndexBusyException exception)
        {
            console.WriteStatus(exception.Message);
            return;
        }

        List<DevCraftFeature> movable = index.Features
            .Where(feature => !feature.Status.Equals("Complete", StringComparison.OrdinalIgnoreCase)
                && FeatureStorageMode.IsLocal(feature.StorageType)
                && !feature.StorageType.Equals(target, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (movable.Count == 0)
        {
            return;
        }

        IReadOnlyList<string> selected = console.SelectMany(
            $"Move existing features to {target}? Select none to leave them where they are.",
            movable.Select(Choice).ToList());

        if (selected.Count == 0)
        {
            console.WriteStatus("No features were moved.");
            return;
        }

        List<string> moved = [];
        List<string> failed = [];

        foreach (DevCraftFeature feature in movable.Where(feature => selected.Contains(Choice(feature))))
        {
            FeatureMoveResult result = FeatureMover.Move(context, index, feature, target);
            index = result.Index;

            if (result.Moved)
            {
                moved.Add(result.Error is null ? feature.Name : $"{feature.Name} ({result.Error})");
            }
            else
            {
                failed.Add($"{feature.Name} ({result.Error})");
            }
        }

        List<string> skipped = movable.Where(feature => !selected.Contains(Choice(feature))).Select(feature => feature.Name).ToList();
        List<string> report = [];

        if (moved.Count > 0)
        {
            report.Add($"Moved to {target}: {string.Join(", ", moved)}.");
        }

        if (skipped.Count > 0)
        {
            report.Add($"Not moved: {string.Join(", ", skipped)}.");
        }

        if (failed.Count > 0)
        {
            report.Add($"Failed: {string.Join(", ", failed)}.");
        }

        console.WriteStatus(string.Join(' ', report));
    }

    private static string Choice(DevCraftFeature feature)
    {
        return $"{feature.Name} ({feature.Slug})";
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
