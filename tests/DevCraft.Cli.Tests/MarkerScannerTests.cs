using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class MarkerScannerTests
{
    [Fact]
    public void ScanDetectsDevCraftAsSdlcWorkflow()
    {
        using TestDirectory root = new();
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));

        IReadOnlyList<MarkerDetection> detections = MarkerScanner.Scan(root.Path);

        MarkerDetection detection = Assert.Single(detections);
        Assert.Equal("DevCraft", detection.Name);
        Assert.Equal(MarkerCategory.SdlcWorkflow, detection.Category);
        Assert.True(detection.IsDevCraft);
    }

    [Fact]
    public void ScanDetectsSpecKitAsSdlcWorkflow()
    {
        using TestDirectory root = new();
        Directory.CreateDirectory(Path.Combine(root.Path, ".specify"));

        IReadOnlyList<MarkerDetection> detections = MarkerScanner.Scan(root.Path);

        MarkerDetection detection = Assert.Single(detections);
        Assert.Equal("Spec Kit", detection.Name);
        Assert.Equal(MarkerCategory.SdlcWorkflow, detection.Category);
        Assert.False(detection.IsDevCraft);
    }

    [Fact]
    public void ScanDetectsClaudeCodeAsAgentConfiguration()
    {
        using TestDirectory root = new();
        File.WriteAllText(Path.Combine(root.Path, "CLAUDE.md"), "# Claude");

        IReadOnlyList<MarkerDetection> detections = MarkerScanner.Scan(root.Path);

        MarkerDetection detection = Assert.Single(detections);
        Assert.Equal("Claude Code", detection.Name);
        Assert.Equal(MarkerCategory.AgentConfiguration, detection.Category);
    }

    [Fact]
    public void ScanDetectsGitHubCopilotAsAgentConfiguration()
    {
        using TestDirectory root = new();
        string github = Path.Combine(root.Path, ".github");
        Directory.CreateDirectory(github);
        File.WriteAllText(Path.Combine(github, "copilot-instructions.md"), "# Copilot");

        IReadOnlyList<MarkerDetection> detections = MarkerScanner.Scan(root.Path);

        MarkerDetection detection = Assert.Single(detections);
        Assert.Equal("GitHub Copilot", detection.Name);
        Assert.Equal(MarkerCategory.AgentConfiguration, detection.Category);
    }

    [Fact]
    public void ScanDetectsAgentsAsAgentConfiguration()
    {
        using TestDirectory root = new();
        File.WriteAllText(Path.Combine(root.Path, "AGENTS.md"), "# Agents");

        IReadOnlyList<MarkerDetection> detections = MarkerScanner.Scan(root.Path);

        MarkerDetection detection = Assert.Single(detections);
        Assert.Equal("Codex / AGENTS", detection.Name);
        Assert.Equal(MarkerCategory.AgentConfiguration, detection.Category);
    }
}

