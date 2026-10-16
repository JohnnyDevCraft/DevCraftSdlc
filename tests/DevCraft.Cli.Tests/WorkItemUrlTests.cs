using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class WorkItemUrlTests
{
    [Theory]
    [InlineData("https://dev.azure.com/org/p/_workitems/edit/1234", true)]
    [InlineData("http://jira.internal/browse/PROJ-1", true)]
    [InlineData("HTTPS://github.com/org/repo/issues/57", true)]
    [InlineData("github.com/org/repo/issues/57", false)]
    [InlineData("/browse/PROJ-1", false)]
    [InlineData("ftp://files.example.com/item", false)]
    [InlineData("", false)]
    [InlineData("not a url", false)]
    public void IsValid(string value, bool expected)
    {
        Assert.Equal(expected, WorkItemUrl.IsValid(value));
    }
}
