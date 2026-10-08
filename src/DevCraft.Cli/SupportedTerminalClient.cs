namespace DevCraft.Cli;

public sealed record SupportedTerminalClient(
    string Slug,
    string Name,
    string Description,
    TerminalClientOperation Scan,
    TerminalClientOperation Session);
