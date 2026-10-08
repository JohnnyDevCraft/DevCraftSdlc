namespace DevCraft.Cli;

public sealed record DevCraftInstallationResult(
    string ControlDirectory,
    IReadOnlyList<string> CreatedPaths,
    IReadOnlyList<string> PreservedPaths);
