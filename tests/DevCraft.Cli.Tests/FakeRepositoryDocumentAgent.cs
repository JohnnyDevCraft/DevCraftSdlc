using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class FakeRepositoryDocumentAgent : IRepositoryDocumentAgent
{
    public string Response { get; set; } = """{ "success": true, "summary": "", "files": [] }""";

    public List<string> Prompts { get; } = [];

    public List<SupportedTerminalClient> Clients { get; } = [];

    public string Run(StartupContext context, SupportedTerminalClient client, string prompt)
    {
        Clients.Add(client);
        Prompts.Add(prompt);

        return Response;
    }
}
