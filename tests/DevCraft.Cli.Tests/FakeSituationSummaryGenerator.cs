using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class FakeSituationSummaryGenerator : ISituationSummaryGenerator
{
    public string Response { get; set; } = """{ "summaryData": "Compressed situation summary." }""";

    public int CallCount { get; private set; }

    public List<string> Prompts { get; } = [];

    public string Generate(StartupContext context, SupportedTerminalClient client, string prompt)
    {
        CallCount++;
        Prompts.Add(prompt);

        return Response;
    }
}
