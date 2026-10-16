using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class FakeDevCraftRepositoryUpgradeLauncher : IDevCraftRepositoryUpgradeLauncher
{
    public DevCraftRepositoryUpgradeResult Result { get; set; } = DevCraftRepositoryUpgradeResult.Succeeded("Upgraded.");

    public List<DevCraftRepositoryUpgradeRequest> Requests { get; } = [];

    // Simulates what the upgrade agent writes before it reports back.
    public Action<DevCraftRepositoryUpgradeRequest>? OnUpgrade { get; set; }

    public DevCraftRepositoryUpgradeResult Upgrade(DevCraftRepositoryUpgradeRequest request)
    {
        Requests.Add(request);
        OnUpgrade?.Invoke(request);

        return Result;
    }
}
