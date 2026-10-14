namespace DevCraft.Cli;

public static class AgentHandoffPrompter
{
    public const string StartingPromptTitle = "Starting Prompt";

    // The starting prompt is collected before the client so the operator states intent first.
    public static AgentHandoff Resolve(IConsoleInteraction console, Func<SupportedTerminalClient> selectClient)
    {
        TextEditResult startingPrompt = console.EditText(new TextEditOptions(StartingPromptTitle, string.Empty));

        if (!startingPrompt.Saved)
        {
            throw new AgentHandoffCancelledException();
        }

        return new AgentHandoff(selectClient(), startingPrompt.Text);
    }
}
