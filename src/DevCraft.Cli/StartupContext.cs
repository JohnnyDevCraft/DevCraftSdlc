namespace DevCraft.Cli;

public sealed record StartupContext(
    string CurrentDirectory,
    string ProfileDirectory,
    string SoulFilePath);

