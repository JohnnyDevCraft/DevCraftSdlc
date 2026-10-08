namespace DevCraft.Cli;

public sealed record SoulSetupAnswers(
    string OperatorName,
    string WorkAndAssistanceContext,
    string AssistantName,
    string ResponseStyle,
    string DefaultTerminalAgent);

