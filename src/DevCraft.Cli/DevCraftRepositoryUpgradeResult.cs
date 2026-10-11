namespace DevCraft.Cli;

public sealed record DevCraftRepositoryUpgradeResult(
    bool Success,
    string Summary,
    IReadOnlyList<string> MovedFeatures,
    IReadOnlyList<string> SkippedFeatures)
{
    public static DevCraftRepositoryUpgradeResult Failed(string summary)
    {
        return new DevCraftRepositoryUpgradeResult(false, summary, [], []);
    }

    public static DevCraftRepositoryUpgradeResult Succeeded(string summary)
    {
        return new DevCraftRepositoryUpgradeResult(true, summary, [], []);
    }
}
