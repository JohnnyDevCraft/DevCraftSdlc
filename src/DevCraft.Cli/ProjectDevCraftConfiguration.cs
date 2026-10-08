namespace DevCraft.Cli;

public sealed record ProjectDevCraftConfiguration(
    string ProjectKey,
    string? SelectedFeatureStorage,
    string FeaturesIndexPath,
    ProjectProfile ProjectProfile,
    IReadOnlyList<DevCraftFeature> Features);
