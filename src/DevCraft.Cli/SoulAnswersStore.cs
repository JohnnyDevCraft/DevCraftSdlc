namespace DevCraft.Cli;

public static class SoulAnswersStore
{
    internal const string OperatorNamePrefix = "- Name:";
    internal const string WorkAndAssistanceContextPrefix = "- Work and assistance context:";
    internal const string AssistantNamePrefix = "- Assistant name:";
    internal const string ResponseStylePrefix = "- Response style:";
    internal const string DefaultTerminalAgentPrefix = "- Default terminal AI agent:";

    public static SoulSetupAnswers Read(string soulFilePath)
    {
        IReadOnlyList<string> lines = File.ReadAllLines(soulFilePath);

        return new SoulSetupAnswers(
            ReadValue(lines, OperatorNamePrefix),
            ReadValue(lines, WorkAndAssistanceContextPrefix),
            ReadValue(lines, AssistantNamePrefix),
            ReadValue(lines, ResponseStylePrefix),
            ReadValue(lines, DefaultTerminalAgentPrefix));
    }

    // Replaces only the known answer lines so hand-written soul.md content survives an edit.
    public static void Save(string soulFilePath, SoulSetupAnswers answers)
    {
        List<string> lines = File.ReadAllLines(soulFilePath).ToList();

        ReplaceOrAppend(lines, OperatorNamePrefix, answers.OperatorName);
        ReplaceOrAppend(lines, WorkAndAssistanceContextPrefix, answers.WorkAndAssistanceContext);
        ReplaceOrAppend(lines, AssistantNamePrefix, answers.AssistantName);
        ReplaceOrAppend(lines, ResponseStylePrefix, answers.ResponseStyle);
        ReplaceOrAppend(lines, DefaultTerminalAgentPrefix, answers.DefaultTerminalAgent);

        File.WriteAllText(soulFilePath, string.Join(Environment.NewLine, lines) + Environment.NewLine);
    }

    internal static void ReplaceOrAppend(List<string> lines, string prefix, string value)
    {
        int index = lines.FindIndex(line => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        string replacement = $"{prefix} {SingleLine(value)}";

        if (index >= 0)
        {
            lines[index] = replacement;
        }
        else
        {
            lines.Add(replacement);
        }
    }

    private static string ReadValue(IReadOnlyList<string> lines, string prefix)
    {
        string? line = lines.FirstOrDefault(value => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        return line is null ? string.Empty : line[prefix.Length..].Trim();
    }

    private static string SingleLine(string value)
    {
        return string.Join(' ', value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}
