namespace DevCraft.Cli;

public interface IDevCraftAiSessionLauncher
{
    void Launch(
        StartupContext context,
        ProjectDevCraftConfiguration projectConfiguration,
        SupportedTerminalClient client,
        string instruction);
}
