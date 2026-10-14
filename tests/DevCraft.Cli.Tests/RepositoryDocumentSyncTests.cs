using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class RepositoryDocumentSyncTests
{
    private static readonly IReadOnlyList<string> Allowed = ["GOV.md", "ARCH.md"];

    [Fact]
    public void ApplyWritesChangedAllowedFiles()
    {
        using TestDirectory root = new();
        string devcraft = Path.Combine(root.Path, ".devcraft");
        Directory.CreateDirectory(devcraft);
        File.WriteAllText(Path.Combine(devcraft, "GOV.md"), "# GOV.md\n\nOld\n");

        RepositoryDocumentSyncResult result = RepositoryDocumentSync.Apply(
            """{"success":true,"summary":"Applied principles.","files":[{"name":"GOV.md","content":"# GOV.md\n\nNew\n"},{"name":"arch.md","content":"# ARCH.md\n"}]}""",
            devcraft,
            Allowed);

        Assert.True(result.Success);
        Assert.Equal("Applied principles.", result.Summary);
        Assert.Equal(["GOV.md", "ARCH.md"], result.UpdatedFiles);
        Assert.Equal("# GOV.md\n\nNew\n", File.ReadAllText(Path.Combine(devcraft, "GOV.md")));
        Assert.Equal("# ARCH.md\n", File.ReadAllText(Path.Combine(devcraft, "ARCH.md")));
    }

    [Fact]
    public void ApplySkipsFilesWhoseContentIsUnchanged()
    {
        using TestDirectory root = new();
        string devcraft = Path.Combine(root.Path, ".devcraft");
        Directory.CreateDirectory(devcraft);
        File.WriteAllText(Path.Combine(devcraft, "GOV.md"), "# GOV.md\r\n\r\nSame\r\n");

        RepositoryDocumentSyncResult result = RepositoryDocumentSync.Apply(
            """{"success":true,"summary":"","files":[{"name":"GOV.md","content":"# GOV.md\n\nSame"}]}""",
            devcraft,
            Allowed);

        Assert.True(result.Success);
        Assert.Empty(result.UpdatedFiles);
        Assert.Equal("# GOV.md\r\n\r\nSame\r\n", File.ReadAllText(Path.Combine(devcraft, "GOV.md")));
    }

    [Theory]
    [InlineData("../GOV.md")]
    [InlineData("src/Program.cs")]
    [InlineData("AGENT.md")]
    public void ApplyRejectsNamesOutsideTheAllowList(string name)
    {
        using TestDirectory root = new();
        string devcraft = Path.Combine(root.Path, ".devcraft");
        Directory.CreateDirectory(devcraft);

        RepositoryDocumentSyncResult result = RepositoryDocumentSync.Apply(
            $$"""{"success":true,"summary":"","files":[{"name":"{{name}}","content":"x"}]}""",
            devcraft,
            Allowed);

        Assert.Empty(result.UpdatedFiles);
        Assert.Equal([name], result.SkippedFiles);
        Assert.Empty(Directory.GetFiles(root.Path, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public void ApplyExtractsJsonWrappedInProseAndFences()
    {
        using TestDirectory root = new();
        string devcraft = Path.Combine(root.Path, ".devcraft");
        Directory.CreateDirectory(devcraft);

        RepositoryDocumentSyncResult result = RepositoryDocumentSync.Apply(
            "```json\n{\"success\":true,\"summary\":\"ok\",\"files\":[{\"name\":\"ARCH.md\",\"content\":\"A\"}]}\n```",
            devcraft,
            Allowed);

        Assert.Equal(["ARCH.md"], result.UpdatedFiles);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("""{"success":false,"summary":"No constitution principles found.","files":[{"name":"GOV.md","content":"x"}]}""")]
    public void ApplyWritesNothingOnFailureOrInvalidJson(string response)
    {
        using TestDirectory root = new();
        string devcraft = Path.Combine(root.Path, ".devcraft");
        Directory.CreateDirectory(devcraft);

        RepositoryDocumentSyncResult result = RepositoryDocumentSync.Apply(response, devcraft, Allowed);

        Assert.False(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.Summary));
        Assert.Empty(Directory.GetFiles(devcraft));
    }
}
