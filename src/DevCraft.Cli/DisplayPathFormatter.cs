namespace DevCraft.Cli;

internal static class DisplayPathFormatter
{
    public static string ToDisplayPath(string path)
    {
        string fullPath = Path.GetFullPath(path);
        string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (string.IsNullOrWhiteSpace(homePath))
        {
            return fullPath;
        }

        string fullHomePath = Path.GetFullPath(homePath).TrimEnd(Path.DirectorySeparatorChar);

        if (string.Equals(fullPath, fullHomePath, StringComparison.Ordinal))
        {
            return "~";
        }

        string homePrefix = fullHomePath + Path.DirectorySeparatorChar;

        return fullPath.StartsWith(homePrefix, StringComparison.Ordinal)
            ? "~" + Path.DirectorySeparatorChar + fullPath[homePrefix.Length..]
            : fullPath;
    }
}
