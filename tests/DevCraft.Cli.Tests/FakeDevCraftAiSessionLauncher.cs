using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class FakeDevCraftAiSessionLauncher : IDevCraftAiSessionLauncher
{
    public Exception? ExceptionToThrow { get; set; }

    public List<(SupportedTerminalClient Client, string Instruction)> Launches { get; } = [];

    public IEnumerable<string> Instructions => Launches.Select(launch => launch.Instruction);

    public void Launch(
        StartupContext context,
        ProjectDevCraftConfiguration projectConfiguration,
        SupportedTerminalClient client,
        string instruction)
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        Launches.Add((client, instruction));
    }
}
