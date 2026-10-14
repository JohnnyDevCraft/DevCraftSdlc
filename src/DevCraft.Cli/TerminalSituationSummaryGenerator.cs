namespace DevCraft.Cli;

public sealed class TerminalSituationSummaryGenerator : ISituationSummaryGenerator
{
    public string Generate(StartupContext context, SupportedTerminalClient client, string prompt)
    {
        return TerminalScanRunner.Run(context, client, prompt, "compression");
    }
}
