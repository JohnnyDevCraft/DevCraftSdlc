namespace DevCraft.Cli;

public static class MarkerCatalog
{
    public static IReadOnlyList<MarkerDefinition> Definitions { get; } =
    [
        new MarkerDefinition(
            "DevCraft",
            MarkerCategory.SdlcWorkflow,
            [".devcraft", ".devcraft/AGENT.md", ".devcraft/features"]),
        new MarkerDefinition(
            "Spec Kit",
            MarkerCategory.SdlcWorkflow,
            [".specify", ".specify/memory/constitution.md", ".specify/templates", "specs"]),
        new MarkerDefinition(
            "Claude Code",
            MarkerCategory.AgentConfiguration,
            ["CLAUDE.md", ".claude/settings.json", ".claude/commands", ".claude/agents", ".claude/skills"]),
        new MarkerDefinition(
            "GitHub Copilot",
            MarkerCategory.AgentConfiguration,
            [".github/copilot-instructions.md", ".github/instructions"]),
        new MarkerDefinition(
            "Codex / AGENTS",
            MarkerCategory.AgentConfiguration,
            ["AGENTS.md", "AGENTS.override.md", ".codex"]),
    ];
}

