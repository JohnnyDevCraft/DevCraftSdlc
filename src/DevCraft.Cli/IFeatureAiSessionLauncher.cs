namespace DevCraft.Cli;

public interface IFeatureAiSessionLauncher
{
    void Launch(
        StartupContext context,
        ProjectDevCraftConfiguration projectConfiguration,
        DevCraftFeature feature,
        AgentHandoff handoff,
        string instruction);
}
