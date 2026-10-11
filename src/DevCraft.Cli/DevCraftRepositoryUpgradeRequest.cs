namespace DevCraft.Cli;

public sealed record DevCraftRepositoryUpgradeRequest(
    StartupContext Context,
    ProjectDevCraftConfiguration ProjectConfiguration,
    string CurrentVersion,
    string? RepositoryVersion,
    string EffectiveFeatureStorage);
