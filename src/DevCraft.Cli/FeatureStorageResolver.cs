namespace DevCraft.Cli;

public static class FeatureStorageResolver
{
    public static string Resolve(
        DevCraftProfileConfiguration profileConfiguration,
        ProjectDevCraftConfiguration projectConfiguration)
    {
        return string.IsNullOrWhiteSpace(projectConfiguration.SelectedFeatureStorage)
            ? profileConfiguration.DefaultFeatureStorage
            : projectConfiguration.SelectedFeatureStorage.Trim();
    }

    public static ProjectDevCraftConfiguration WithEffectiveStorage(
        DevCraftProfileConfiguration profileConfiguration,
        ProjectDevCraftConfiguration projectConfiguration)
    {
        return projectConfiguration with
        {
            SelectedFeatureStorage = Resolve(profileConfiguration, projectConfiguration),
        };
    }
}
