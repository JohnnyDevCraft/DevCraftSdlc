namespace DevCraft.Cli;

public static class FeatureStorageMode
{
    public const string RepoCentral = "repo-central";
    public const string SystemCentral = "system-central";

    public static bool IsLocal(string value)
    {
        return value.Equals(RepoCentral, StringComparison.OrdinalIgnoreCase) ||
            value.Equals(SystemCentral, StringComparison.OrdinalIgnoreCase);
    }
}

