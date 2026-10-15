using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class BranchNameResolverTests
{
    [Fact]
    public void TokensAreDistinctOrderedAndCaseInsensitive()
    {
        IReadOnlyList<string> tokens = BranchNameResolver.Tokens("{adUser}/{FeatureType}/{WorkITem}-{Title}-{workitem}");

        Assert.Equal(["adUser", "FeatureType", "WorkITem", "Title"], tokens);
    }

    [Fact]
    public void ResolveReplacesTokensCaseInsensitivelyWithSanitizedValues()
    {
        BranchNameResolution resolution = BranchNameResolver.Resolve(
            "{adUser}/{FeatureType}/{WorkITem}-{Title}",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["adUser"] = "jdoe",
                ["FeatureType"] = "bug",
                ["WorkItem"] = "1234",
                ["Title"] = "Fix login page",
            });

        Assert.Equal("jdoe/bug/1234-Fix-login-page", resolution.BranchName);
        Assert.Empty(resolution.UnresolvedTokens);
    }

    [Fact]
    public void ResolveReportsUnresolvedTokensAndLeavesThemInPlace()
    {
        BranchNameResolution resolution = BranchNameResolver.Resolve(
            "{Team}/{Title}",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Title"] = "x" });

        Assert.Equal(["Team"], resolution.UnresolvedTokens);
        Assert.Equal("{Team}/x", resolution.BranchName);
    }

    [Theory]
    [InlineData("  Fix   the bug  ", "Fix-the-bug")]
    [InlineData("a/b\\c", "a-b-c")]
    [InlineData("what? *now* [ok] ~^:", "what-now-ok")]
    [InlineData("v1..2 @{x}", "v1.2-x")]
    [InlineData("--.edge.--", "edge")]
    [InlineData("tab\there", "tab-here")]
    public void SanitizeProducesRefSafeSegments(string value, string expected)
    {
        Assert.Equal(expected, BranchNameResolver.Sanitize(value));
    }

    [Fact]
    public void TitleIsCappedAtFiftyFiveCharacters()
    {
        string title = new string('a', 50) + " bbbbbbbbbb";

        BranchNameResolution resolution = BranchNameResolver.Resolve(
            "{Title}",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Title"] = title });

        Assert.Equal(55, resolution.BranchName.Length);
        Assert.Equal(new string('a', 50) + "-bbbb", resolution.BranchName);
    }

    [Fact]
    public void TitleCapDoesNotLeaveTrailingSeparator()
    {
        string title = new string('a', 54) + " b";

        Assert.Equal(new string('a', 54), BranchNameResolver.Resolve(
            "{Title}",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Title"] = title }).BranchName);
    }
}
