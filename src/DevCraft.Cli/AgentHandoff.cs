namespace DevCraft.Cli;

public sealed record AgentHandoff(
    SupportedTerminalClient Client,
    string StartingPrompt);
