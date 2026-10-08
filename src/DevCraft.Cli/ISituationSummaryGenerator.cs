namespace DevCraft.Cli;

public interface ISituationSummaryGenerator
{
    string Generate(StartupContext context, SupportedTerminalClient client, string prompt);
}
