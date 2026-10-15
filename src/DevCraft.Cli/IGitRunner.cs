namespace DevCraft.Cli;

public interface IGitRunner
{
    TerminalProcessResult Run(string workingDirectory, IReadOnlyList<string> arguments);
}
