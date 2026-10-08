using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class ProjectScanPromptBuilderTests
{
    [Fact]
    public void BuildAsksForProjectsAndAiDrivenSdlc()
    {
        string prompt = ProjectScanPromptBuilder.Build("/tmp/sample");

        Assert.Contains("/tmp/sample", prompt);
        Assert.Contains("project names", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("project types", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("overall project name", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("what the project is for", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Superpowers", prompt);
        Assert.Contains("SpecKit", prompt);
        Assert.Contains("\"projectProfile\"", prompt);
        Assert.Contains("\"projects\"", prompt);
        Assert.Contains("\"aiDrivenSdlc\"", prompt);
    }
}
