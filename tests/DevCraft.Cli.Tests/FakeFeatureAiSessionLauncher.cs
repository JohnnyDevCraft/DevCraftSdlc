using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class FakeFeatureAiSessionLauncher : IFeatureAiSessionLauncher
{
    public Exception? ExceptionToThrow { get; set; }

    public List<(DevCraftFeature Feature, SupportedTerminalClient Client, string Instruction)> Launches { get; } = [];
    public List<ProjectDevCraftConfiguration> ProjectConfigurations { get; } = [];

    public void Launch(
        StartupContext context,
        ProjectDevCraftConfiguration projectConfiguration,
        DevCraftFeature feature,
        SupportedTerminalClient client,
        string instruction)
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        Launches.Add((feature, client, instruction));
        ProjectConfigurations.Add(projectConfiguration);
    }
}
