namespace DevCraft.Cli;

public sealed record MarkerDefinition(
    string Name,
    MarkerCategory Category,
    IReadOnlyList<string> Paths);

