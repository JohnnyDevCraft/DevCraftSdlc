namespace DevCraft.Cli;

public sealed record ProfileState(
    string ProfileDirectory,
    string SoulFilePath,
    bool ProfileDirectoryExists,
    bool SoulFileExists);

