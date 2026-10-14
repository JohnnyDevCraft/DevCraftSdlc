namespace DevCraft.Cli;

public interface IRepositoryDocumentAgent
{
    string Run(StartupContext context, SupportedTerminalClient client, string prompt);
}
