using System.Security.Cryptography;

namespace DevCraft.Cli;

// Moves a feature's artifacts between Repo Central and System Central: copy, verify each file by size and
// SHA-256, record the new storage type, and only then delete the source. Any failure leaves the source as it was.
public static class FeatureMover
{
    public static FeatureMoveResult Move(
        StartupContext context,
        FeatureIndex index,
        DevCraftFeature feature,
        string targetStorage,
        Action<string, string>? copyFile = null)
    {
        string? source = FeatureDirectories.For(context, feature.StorageType, feature.FolderName);
        string? target = FeatureDirectories.For(context, targetStorage, feature.FolderName);

        if (source is null || target is null)
        {
            return new FeatureMoveResult(index, false, "only Repo Central and System Central features can be moved.");
        }

        if (Directory.Exists(target))
        {
            return new FeatureMoveResult(index, false, "the target folder already exists.");
        }

        try
        {
            Directory.CreateDirectory(target);

            if (Directory.Exists(source) && CopyAndVerify(source, target, copyFile ?? ((from, to) => File.Copy(from, to))) is string problem)
            {
                Directory.Delete(target, recursive: true);
                return new FeatureMoveResult(index, false, problem);
            }

            index = index.Save(feature with { StorageType = targetStorage });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ProjectsIndexBusyException)
        {
            if (Directory.Exists(target))
            {
                Directory.Delete(target, recursive: true);
            }

            return new FeatureMoveResult(index, false, exception.Message);
        }

        // The index already points at the new folder, so a leftover source folder is reported, never rolled back.
        try
        {
            if (Directory.Exists(source))
            {
                Directory.Delete(source, recursive: true);
            }

            return new FeatureMoveResult(index, true, null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new FeatureMoveResult(index, true, $"the old folder {source} could not be removed: {exception.Message}");
        }
    }

    private static string? CopyAndVerify(string source, string target, Action<string, string> copyFile)
    {
        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(source, file);
            string destination = Path.Combine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            copyFile(file, destination);

            if (!File.Exists(destination) || !SameContent(file, destination))
            {
                return $"{relative} did not copy correctly.";
            }
        }

        return null;
    }

    private static bool SameContent(string first, string second)
    {
        if (new FileInfo(first).Length != new FileInfo(second).Length)
        {
            return false;
        }

        using FileStream firstStream = File.OpenRead(first);
        using FileStream secondStream = File.OpenRead(second);

        return SHA256.HashData(firstStream).AsSpan().SequenceEqual(SHA256.HashData(secondStream));
    }
}
