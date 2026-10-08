namespace DevCraft.Cli;

public static class TerminalAgentCatalog
{
    public static IReadOnlyList<TerminalAgent> Create(Func<string, bool> commandExists)
    {
        return
        [
            new TerminalAgent(TerminalAgentKind.Codex, "Codex", "codex", commandExists("codex")),
            new TerminalAgent(TerminalAgentKind.Claude, "Claude AI", "claude", commandExists("claude")),
            new TerminalAgent(TerminalAgentKind.GitHubCopilot, "GitHub Copilot", "gh copilot", commandExists("gh")),
        ];
    }
}

