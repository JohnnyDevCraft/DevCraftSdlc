namespace DevCraft.Cli;

public sealed record DevCraftProfileConfiguration(
    IReadOnlyList<ProfileCatalogDocument> Skills,
    IReadOnlyList<ProfileCatalogDocument> Standards,
    IReadOnlyList<ProfileCatalogDocument> Architectures,
    IReadOnlyList<ProfileCatalogDocument> Templates,
    IReadOnlyList<ProfileCatalogDocument> ProjectTypes,
    IReadOnlyList<ProfileCatalogDocument> FeatureStorageTypes,
    string SelectedFeatureStorage,
    IReadOnlyList<SupportedTerminalClient> SupportedClients);
