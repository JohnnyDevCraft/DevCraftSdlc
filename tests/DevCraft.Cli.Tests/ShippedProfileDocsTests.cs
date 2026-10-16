namespace DevCraft.Cli.Tests;

// The installer copies the repository profile folder into the operator's .DevCraft folder, so these
// documents are what agents read about feature storage.
public sealed class ShippedProfileDocsTests
{
    [Fact]
    public void SystemCentral_DescribesCentralIndexWorkingCopiesAndLocking()
    {
        string text = Read("feature-storage", "system-central.md");

        Assert.Contains("`working-copies`", text);
        Assert.Contains("`repo-remote`", text);
        Assert.Contains("`work-item-id`", text);
        Assert.Contains("`branch`", text);
        Assert.Contains("projects.json.lock", text);
        Assert.Contains("Set Feature to Current Branch", text);
        Assert.DoesNotContain("## Both Indexes", text);
    }

    [Theory]
    [InlineData("ado-work-item.md")]
    [InlineData("github-issue.md")]
    [InlineData("jira-work-item.md")]
    public void TrackerStorage_UsesProjectsJsonIndex(string file)
    {
        string text = Read("feature-storage", file);

        Assert.DoesNotContain(".devcraft/features.json", text);
        Assert.Contains("projects.json", text);
    }

    [Fact]
    public void DevCraftRules_ConfigureJsonListsOnlyRepoCentralFeatures()
    {
        Assert.Contains("`Features` lists only repo-central features", Read("DevCraft.md"));
    }

    private static string Read(params string[] parts)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DevCraft.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);

        return File.ReadAllText(Path.Combine([directory.FullName, "profile", .. parts]));
    }
}
