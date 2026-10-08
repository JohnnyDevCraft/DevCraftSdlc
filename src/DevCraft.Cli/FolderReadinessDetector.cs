namespace DevCraft.Cli;

public static class FolderReadinessDetector
{
    public static FolderReadinessResult Detect(string directoryPath)
    {
        bool hasNonHiddenFile = HasNonHiddenFile(directoryPath);

        return new FolderReadinessResult(
            directoryPath,
            hasNonHiddenFile ? FolderReadiness.HasCode : FolderReadiness.ReadyForDevCraft);
    }

    private static bool HasNonHiddenFile(string directoryPath)
    {
        foreach (string file in Directory.EnumerateFiles(directoryPath))
        {
            if (!IsHidden(file))
            {
                return true;
            }
        }

        foreach (string directory in Directory.EnumerateDirectories(directoryPath))
        {
            if (ShouldSkipDirectory(directory))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static bool ShouldSkipDirectory(string path)
    {
        if (IsHidden(path))
        {
            return true;
        }

        string name = Path.GetFileName(path);

        return name is "bin" or "obj" or "node_modules";
    }

    private static bool IsHidden(string path)
    {
        string name = Path.GetFileName(path);

        if (name.StartsWith(".", StringComparison.Ordinal))
        {
            return true;
        }

        FileAttributes attributes = File.GetAttributes(path);

        return attributes.HasFlag(FileAttributes.Hidden);
    }
}
