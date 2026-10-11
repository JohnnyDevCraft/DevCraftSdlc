namespace DevCraft.Cli;

public interface IDevCraftRepositoryUpgradeLauncher
{
    DevCraftRepositoryUpgradeResult Upgrade(DevCraftRepositoryUpgradeRequest request);
}
