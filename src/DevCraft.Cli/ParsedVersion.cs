namespace DevCraft.Cli;

internal readonly record struct ParsedVersion(
    int Major,
    int Minor,
    int Patch,
    string? Prerelease);
