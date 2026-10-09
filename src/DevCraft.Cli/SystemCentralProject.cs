namespace DevCraft.Cli;

public sealed record SystemCentralProject(
    string ProjectKey,
    string Name,
    string Slug,
    string RepositoryPath,
    IReadOnlyList<DevCraftFeature> Features)
{
    public string RepositoryName
    {
        get
        {
            string path = RepositoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : Path.GetFileName(path);
        }
    }
}
