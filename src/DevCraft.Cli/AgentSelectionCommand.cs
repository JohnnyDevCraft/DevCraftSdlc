namespace DevCraft.Cli;

public static class AgentSelectionCommand
{
    private const string CancelChoice = "Cancel";

    public static void Run(
        StartupContext context,
        IConsoleInteraction console,
        Func<string, bool> commandExists)
    {
        ProfileStructureInitializer.Ensure(context.ProfileDirectory);
        IReadOnlyList<SupportedTerminalClient> clients = ProfileConfigurationReader.Read(context.ProfileDirectory).SupportedClients;
        IReadOnlyList<SupportedTerminalClient> installedClients = clients
            .Where(client => commandExists(client.Scan.BinaryPath))
            .ToList();

        if (installedClients.Count == 0)
        {
            console.WriteStatus("No installed terminal AI agents were found on PATH. Default AI agent was not changed.");
            return;
        }

        IReadOnlyDictionary<string, SupportedTerminalClient> choices = installedClients.ToDictionary(ClientChoice, StringComparer.Ordinal);
        string selected = console.Select(
            "Which installed terminal AI agent should DevCraft use by default?",
            choices.Keys.Concat([CancelChoice]).ToList());

        if (selected == CancelChoice)
        {
            console.WriteStatus("Default AI agent was not changed.");
            return;
        }

        SupportedTerminalClient client = choices[selected];
        SoulDefaultAgentStore.Write(context.SoulFilePath, client.Slug);
        console.WriteStatus($"Default AI agent set to {client.Name} ({client.Scan.BinaryPath}).");
    }

    private static string ClientChoice(SupportedTerminalClient client)
    {
        return $"{client.Name} ({client.Scan.BinaryPath})";
    }
}
