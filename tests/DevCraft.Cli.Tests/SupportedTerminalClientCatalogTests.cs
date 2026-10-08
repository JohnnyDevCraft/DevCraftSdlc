using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class SupportedTerminalClientCatalogTests
{
    [Fact]
    public void CreateIncludesInitialSupportedClients()
    {
        IReadOnlyList<SupportedTerminalClient> clients = SupportedTerminalClientCatalog.Create();

        Assert.Contains(clients, client => client.Slug == "codex" && client.Scan.BinaryPath == "codex");
        Assert.Contains(clients, client => client.Slug == "claude-code" && client.Scan.BinaryPath == "claude");
        Assert.Contains(clients, client => client.Slug == "github-copilot" && client.Scan.BinaryPath == "gh");
        SupportedTerminalClient codex = Assert.Single(clients, client => client.Slug == "codex");
        Assert.DoesNotContain("read-only", codex.Scan.Arguments);
        Assert.Contains("--cd", codex.Session.Arguments);
        Assert.Contains("{workingDirectory}", codex.Session.Arguments);
        Assert.All(clients, client => Assert.NotEmpty(client.Scan.Arguments));
        Assert.All(clients, client => Assert.NotEmpty(client.Session.Arguments));
    }
}
