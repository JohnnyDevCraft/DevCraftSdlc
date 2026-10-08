namespace DevCraft.Cli;

public static class SupportedTerminalClientCatalog
{
    public static IReadOnlyList<SupportedTerminalClient> Create()
    {
        return
        [
            new SupportedTerminalClient(
                "codex",
                "OpenAI Codex",
                "OpenAI's terminal coding agent.",
                new TerminalClientOperation(
                    "Run Codex in non-interactive JSON mode for DevCraft folder scanning.",
                    "codex",
                    ["exec", "--json", "--cd", "{workingDirectory}", "{prompt}"]),
                new TerminalClientOperation(
                    "Open an interactive Codex session that the operator can take over.",
                    "codex",
                    ["--cd", "{workingDirectory}", "{prompt}"])),
            new SupportedTerminalClient(
                "claude-code",
                "Claude Code",
                "Anthropic's Claude terminal coding agent.",
                new TerminalClientOperation(
                    "Run Claude Code in print mode for DevCraft folder scanning.",
                    "claude",
                    ["--print", "{prompt}"]),
                new TerminalClientOperation(
                    "Open an interactive Claude Code session that the operator can take over.",
                    "claude",
                    ["{prompt}"])),
            new SupportedTerminalClient(
                "github-copilot",
                "GitHub Copilot",
                "GitHub Copilot terminal integration through the GitHub CLI.",
                new TerminalClientOperation(
                    "Run GitHub Copilot through the GitHub CLI for DevCraft folder scanning.",
                    "gh",
                    ["copilot", "suggest", "{prompt}"]),
                new TerminalClientOperation(
                    "Open a GitHub Copilot CLI interaction that the operator can take over.",
                    "gh",
                    ["copilot", "suggest", "{prompt}"])),
        ];
    }
}
