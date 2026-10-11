using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class DevCraftVersionComparerTests
{
    [Fact]
    public void CompareRepositoryToCurrentTreatsBeta9AsOlderThanBeta10()
    {
        DevCraftVersionComparison comparison = DevCraftVersionComparer.CompareRepositoryToCurrent("1.0.0-beta.9", "1.0.0-beta.10");

        Assert.Equal(DevCraftVersionComparison.Older, comparison);
    }

    [Fact]
    public void CompareRepositoryToCurrentTreatsBeta10AsOlderThanBeta11()
    {
        DevCraftVersionComparison comparison = DevCraftVersionComparer.CompareRepositoryToCurrent("1.0.0-beta.10", "1.0.0-beta.11");

        Assert.Equal(DevCraftVersionComparison.Older, comparison);
    }

    [Fact]
    public void CompareRepositoryToCurrentTreatsMissingVersionAsInvalid()
    {
        DevCraftVersionComparison comparison = DevCraftVersionComparer.CompareRepositoryToCurrent(null, "1.0.0-beta.11");

        Assert.Equal(DevCraftVersionComparison.MissingOrInvalid, comparison);
    }
}
