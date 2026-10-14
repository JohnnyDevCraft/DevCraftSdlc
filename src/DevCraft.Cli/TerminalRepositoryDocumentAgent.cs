namespace DevCraft.Cli;

public sealed class TerminalRepositoryDocumentAgent : IRepositoryDocumentAgent
{
    public string Run(StartupContext context, SupportedTerminalClient client, string prompt)
    {
        return TerminalScanRunner.Run(context, client, prompt, "document update");
    }
}
