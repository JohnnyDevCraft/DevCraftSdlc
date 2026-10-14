namespace DevCraft.Cli;

public static class SoulEditCommand
{
    private const string NotChanged = "Soul was not changed.";

    public static void Run(StartupContext context, IConsoleInteraction console)
    {
        if (!File.Exists(context.SoulFilePath))
        {
            console.WriteStatus($"No soul.md found at {context.SoulFilePath}. Restart DevCraft to run soul setup.");
            return;
        }

        SoulSetupAnswers current = SoulAnswersStore.Read(context.SoulFilePath);

        ValueEditResult operatorName = console.EditValue("Edit Soul - What should I call you?", "Your name", current.OperatorName, required: true);

        if (!operatorName.Saved)
        {
            console.WriteStatus(NotChanged);
            return;
        }

        TextEditResult workContext = console.EditText(new TextEditOptions(
            "Tell me what you do and how I'll be able to assist you.",
            current.WorkAndAssistanceContext));

        if (!workContext.Saved)
        {
            console.WriteStatus(NotChanged);
            return;
        }

        ValueEditResult assistantName = console.EditValue("Edit Soul - What would you like me to be called?", "Assistant name", current.AssistantName, required: true);

        if (!assistantName.Saved)
        {
            console.WriteStatus(NotChanged);
            return;
        }

        TextEditResult responseStyle = console.EditText(new TextEditOptions(
            "How would you like me to respond to you?",
            current.ResponseStyle));

        if (!responseStyle.Saved)
        {
            console.WriteStatus(NotChanged);
            return;
        }

        string defaultAgent = SelectDefaultAgent(context, console, current.DefaultTerminalAgent);
        SoulSetupAnswers updated = new(
            operatorName.Value.Trim(),
            workContext.Text.Trim(),
            assistantName.Value.Trim(),
            responseStyle.Text.Trim(),
            defaultAgent);

        if (updated == current)
        {
            console.WriteStatus(NotChanged);
            return;
        }

        SoulAnswersStore.Save(context.SoulFilePath, updated);
        console.WriteStatus("Soul updated.");
    }

    private static string SelectDefaultAgent(StartupContext context, IConsoleInteraction console, string currentAgent)
    {
        string keepChoice = $"Keep current: {(string.IsNullOrWhiteSpace(currentAgent) ? "(none)" : currentAgent)}";
        IReadOnlyDictionary<string, SupportedTerminalClient> clients = ProfileConfigurationReader
            .Read(context.ProfileDirectory)
            .SupportedClients
            .ToDictionary(client => $"{client.Name} ({client.Slug})", StringComparer.Ordinal);

        string selected = console.Select(
            "Which terminal AI agent should I use by default?",
            [keepChoice, .. clients.Keys]);

        return clients.TryGetValue(selected, out SupportedTerminalClient? client)
            ? client.Slug
            : currentAgent;
    }
}
