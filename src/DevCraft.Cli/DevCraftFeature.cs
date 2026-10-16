namespace DevCraft.Cli;

// ExternalReference is the work item's web URL; WorkItemId is the tracker-agnostic ID it points to.
public sealed record DevCraftFeature(
    string Name,
    string Slug,
    string ShortDescription,
    string FolderName,
    string StorageType,
    string? ExternalReference,
    string Status = "Discovery",
    string? WorkItemId = null,
    string? Branch = null);
