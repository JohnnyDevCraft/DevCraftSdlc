namespace DevCraft.Cli;

public static class FeatureDirectories
{
    // Local artifact folder for a feature, or null for storage types that keep artifacts on a tracker.
    public static string? For(StartupContext context, string storageType, string folderName)
    {
        if (storageType.Equals(FeatureStorageMode.SystemCentral, StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(context.ProfileDirectory, "features", folderName);
        }

        if (storageType.Equals(FeatureStorageMode.RepoCentral, StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(context.CurrentDirectory, ".devcraft", "features", folderName);
        }

        return null;
    }
}
