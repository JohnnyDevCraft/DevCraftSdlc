namespace DevCraft.Cli;

public sealed record SystemCentralProject(
    string ProjectKey,
    string Name,
    string Slug,
    string RepositoryName,
    string? RepositoryRemote,
    IReadOnlyList<WorkingCopy> WorkingCopies,
    IReadOnlyList<DevCraftFeature> Features)
{
    public static string FolderName(string path)
    {
        string trimmed = path.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);

        return string.IsNullOrWhiteSpace(trimmed)
            ? string.Empty
            : System.IO.Path.GetFileName(trimmed);
    }
}
