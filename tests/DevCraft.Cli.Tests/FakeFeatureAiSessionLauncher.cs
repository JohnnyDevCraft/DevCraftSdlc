using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class FakeFeatureAiSessionLauncher : IFeatureAiSessionLauncher
{
    public List<(DevCraftFeature Feature, SupportedTerminalClient Client, string Instruction)> Launches { get; } = [];

    public void Launch(
        StartupContext context,
        ProjectDevCraftConfiguration projectConfiguration,
        DevCraftFeature feature,
        SupportedTerminalClient client,
        string instruction)
    {
        Launches.Add((feature, client, instruction));
    }
}
