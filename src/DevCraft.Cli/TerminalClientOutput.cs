namespace DevCraft.Cli;

public sealed record TerminalClientOutput(
    string Response,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);
