namespace DevCraft.Cli;

public sealed record ProfileCatalogDocument(
    string Slug,
    string Name,
    string Description,
    string Path);
