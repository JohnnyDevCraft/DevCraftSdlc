namespace DevCraft.Cli;

public static class WorkingCopyPaths
{
    // Default macOS and Windows volumes ignore case; Linux file systems do not.
    public static readonly bool IgnoreCase = !OperatingSystem.IsLinux();

    public static string Normalize(string path)
    {
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }

    public static bool Same(string first, string second)
    {
        return Same(first, second, IgnoreCase);
    }

    public static bool Same(string first, string second, bool ignoreCase)
    {
        return string.Equals(
            Normalize(first),
            Normalize(second),
            ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }
}
