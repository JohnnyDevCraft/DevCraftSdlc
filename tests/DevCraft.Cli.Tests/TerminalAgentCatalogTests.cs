using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class TerminalAgentCatalogTests
{
    [Fact]
    public void CreateIncludesCodexClaudeAndGitHubCopilot()
    {
        IReadOnlyList<TerminalAgent> agents = TerminalAgentCatalog.Create(command => command is "codex" or "gh");

        Assert.Contains(agents, agent => agent.DisplayName == "Codex" && agent.IsInstalled);
        Assert.Contains(agents, agent => agent.DisplayName == "Claude AI" && !agent.IsInstalled);
        Assert.Contains(agents, agent => agent.DisplayName == "GitHub Copilot" && agent.IsInstalled);
    }
}

