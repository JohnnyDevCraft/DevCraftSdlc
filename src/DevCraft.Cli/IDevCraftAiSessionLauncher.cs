namespace DevCraft.Cli;

public interface IDevCraftAiSessionLauncher
{
    void Launch(
        StartupContext context,
        ProjectDevCraftConfiguration projectConfiguration,
        AgentHandoff handoff,
        string instruction);
}
