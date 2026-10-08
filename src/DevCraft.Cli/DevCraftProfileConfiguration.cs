namespace DevCraft.Cli;

public sealed record DevCraftProfileConfiguration(
    IReadOnlyList<ProfileCatalogDocument> Skills,
    IReadOnlyList<ProfileCatalogDocument> Standards,
    IReadOnlyList<ProfileCatalogDocument> Architectures,
    IReadOnlyList<ProfileCatalogDocument> Templates,
    IReadOnlyList<ProfileCatalogDocument> ProjectTypes,
    IReadOnlyList<ProfileCatalogDocument> FeatureStorageTypes,
    IReadOnlyList<SupportedTerminalClient> SupportedClients,
    bool SituationEnabled = false,
    string SituationScale = "weeks",
    string SituationStorage = "file",
    string? SituationConnection = null);
