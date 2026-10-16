using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class WorkingCopyNamesTests
{
    [Fact]
    public void SameFolderNameAsRepo_UsesParent()
    {
        IReadOnlyList<string> names = WorkingCopyNames.Shortest(
            ["/repos/Active Work 1/ProjectCodeArc", "/repos/Active Work 2/ProjectCodeArc", "/repos/Active Work 3/ProjectCodeArc"],
            "ProjectCodeArc");

        Assert.Equal(["Active Work 1", "Active Work 2", "Active Work 3"], names);
    }

    [Fact]
    public void UniqueFolders_UseFolderName()
    {
        Assert.Equal(["CodeArc-1", "CodeArc-2"], WorkingCopyNames.Shortest(["/repos/CodeArc-1", "/repos/CodeArc-2"], "CodeArc"));
    }

    [Fact]
    public void ParentAlsoCollides_AddsMoreParents()
    {
        IReadOnlyList<string> names = WorkingCopyNames.Shortest(["/a/Work/Arc", "/b/Work/Arc"], "Arc");

        Assert.Equal(["a/Work/Arc", "b/Work/Arc"], names);
    }

    [Fact]
    public void FolderCollidesButDiffersFromRepo_UsesParentThenFolder()
    {
        IReadOnlyList<string> names = WorkingCopyNames.Shortest(["/one/clone", "/two/clone"], "ProjectCodeArc");

        Assert.Equal(["one", "two"], names);
    }

    [Theory]
    [InlineData("/repos/Active Work 1/ProjectCodeArc", "ProjectCodeArc", "Active Work 1")]
    [InlineData("/repos/CodeArc-1", "ProjectCodeArc", "CodeArc-1")]
    public void SingleCopy_UsesFolderOrParentPerRule(string path, string repoName, string expected)
    {
        Assert.Equal([expected], WorkingCopyNames.Shortest([path], repoName));
    }

    [Fact]
    public void WindowsSeparators_AreSplit()
    {
        Assert.Equal(["Active Work 2"], WorkingCopyNames.Shortest([@"C:\repos\Active Work 2\ProjectCodeArc"], "ProjectCodeArc"));
    }
}
