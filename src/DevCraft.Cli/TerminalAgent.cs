namespace DevCraft.Cli;

public sealed record TerminalAgent(
    TerminalAgentKind Kind,
    string DisplayName,
    string Command,
    bool IsInstalled);

