using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class FakeDevCraftRepositoryUpgradeLauncher : IDevCraftRepositoryUpgradeLauncher
{
    public DevCraftRepositoryUpgradeResult Result { get; set; } = DevCraftRepositoryUpgradeResult.Succeeded("Upgraded.");

    public List<DevCraftRepositoryUpgradeRequest> Requests { get; } = [];

    public DevCraftRepositoryUpgradeResult Upgrade(DevCraftRepositoryUpgradeRequest request)
    {
        Requests.Add(request);

        return Result;
    }
}
