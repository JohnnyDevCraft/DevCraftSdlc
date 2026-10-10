namespace DevCraft.Cli;

internal sealed record CliHeaderContext(
    string DirectoryPath,
    string GitBranch,
    string? GitDivergence);
