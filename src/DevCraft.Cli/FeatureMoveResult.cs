namespace DevCraft.Cli;

public sealed record FeatureMoveResult(FeatureIndex Index, bool Moved, string? Error);
