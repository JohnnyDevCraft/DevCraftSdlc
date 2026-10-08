namespace DevCraft.Cli;

public sealed record SystemCentralProject(
    string ProjectKey,
    string Name,
    string Slug,
    string RepositoryPath,
    IReadOnlyList<DevCraftFeature> Features);

