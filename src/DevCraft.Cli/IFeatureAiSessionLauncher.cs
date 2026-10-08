namespace DevCraft.Cli;

public interface IFeatureAiSessionLauncher
{
    void Launch(
        StartupContext context,
        ProjectDevCraftConfiguration projectConfiguration,
        DevCraftFeature feature,
        SupportedTerminalClient client,
        string instruction);
}
