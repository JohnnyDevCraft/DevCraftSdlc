using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class RepositoryDocumentPromptBuilderTests
{
    [Fact]
    public void ConstitutionImportNamesConstitutionAllowedFilesAndJsonContract()
    {
        string prompt = RepositoryDocumentPromptBuilder.BuildConstitutionImport(
            "/repo",
            "/repo/.specify/memory/constitution.md",
            ["AGENT.md", "GOV.md"]);

        Assert.Contains("/repo/.specify/memory/constitution.md", prompt);
        Assert.Contains(Path.Combine("/repo", ".devcraft"), prompt);
        Assert.Contains("AGENT.md, GOV.md", prompt);
        Assert.Contains("\"files\"", prompt);
        Assert.Contains("Project Status", prompt);
        Assert.Contains("Discovery Stage", prompt);
        Assert.Contains("Do not write files", prompt);
    }

    [Fact]
    public void InferProjectDetailsMentionsConstitutionAndSpecsOnlyWhenPresent()
    {
        string withSpecKit = RepositoryDocumentPromptBuilder.BuildInferProjectDetails(
            "/repo",
            "/repo/.specify/memory/constitution.md",
            "/repo/specs",
            ["ARCH.md", "THEME.md"]);
        string withoutSpecKit = RepositoryDocumentPromptBuilder.BuildInferProjectDetails(
            "/repo",
            null,
            null,
            ["ARCH.md", "THEME.md"]);

        Assert.Contains("/repo/.specify/memory/constitution.md", withSpecKit);
        Assert.Contains("/repo/specs", withSpecKit);
        Assert.DoesNotContain("constitution", withoutSpecKit, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/repo/specs", withoutSpecKit);
        Assert.Contains("THEME.md only", withoutSpecKit);
        Assert.Contains("Project Status", withoutSpecKit);
    }
}
