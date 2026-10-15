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
    string? SituationConnection = null,
    int SchemaVersion = DevCraftProfileConfiguration.CurrentSchemaVersion)
{
    public const int CurrentSchemaVersion = 1;

    public static readonly IReadOnlyList<string> DefaultFeatureTypes = ["enabler", "bug", "story"];

    public string DefaultFeatureStorage { get; init; } = FeatureStorageMode.SystemCentral;

    public IReadOnlyList<string> FeatureTypes { get; init; } = DefaultFeatureTypes;

    public GitToolingSettings GitTooling { get; init; } = new();
}
