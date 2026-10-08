namespace DevCraft.Cli;

public sealed record FolderReadinessResult(
    string DirectoryPath,
    FolderReadiness Readiness);

