namespace DevCraft.Cli;

public sealed record DevCraftFeature(
    string Name,
    string Slug,
    string ShortDescription,
    string FolderName,
    string StorageType,
    string? ExternalReference);
