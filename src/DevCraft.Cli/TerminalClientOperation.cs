namespace DevCraft.Cli;

public sealed record TerminalClientOperation(
    string Description,
    string BinaryPath,
    IReadOnlyList<string> Arguments);
