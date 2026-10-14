using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class FakeFeatureAiSessionLauncher : IFeatureAiSessionLauncher
{
    public Exception? ExceptionToThrow { get; set; }

    public List<(DevCraftFeature Feature, SupportedTerminalClient Client, string Instruction)> Launches { get; } = [];
    public List<ProjectDevCraftConfiguration> ProjectConfigurations { get; } = [];
    public List<string> StartingPrompts { get; } = [];

    public void Launch(
        StartupContext context,
        ProjectDevCraftConfiguration projectConfiguration,
        DevCraftFeature feature,
        AgentHandoff handoff,
        string instruction)
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        Launches.Add((feature, handoff.Client, instruction));
        StartingPrompts.Add(handoff.StartingPrompt);
        ProjectConfigurations.Add(projectConfiguration);
    }
}
