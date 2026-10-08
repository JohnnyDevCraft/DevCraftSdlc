using System.Text;

namespace DevCraft.Cli;

public static class SoulFileWriter
{
    public static void Write(string path, SoulSetupAnswers answers)
    {
        string content = BuildContent(answers);

        File.WriteAllText(path, content, Encoding.UTF8);
    }

    public static string BuildContent(SoulSetupAnswers answers)
    {
        StringBuilder builder = new();

        builder.AppendLine("# SOUL.md");
        builder.AppendLine();
        builder.AppendLine("## Operator");
        builder.AppendLine();
        builder.AppendLine($"- Name: {answers.OperatorName}");
        builder.AppendLine($"- Work and assistance context: {answers.WorkAndAssistanceContext}");
        builder.AppendLine();
        builder.AppendLine("## AI Identity");
        builder.AppendLine();
        builder.AppendLine($"- Assistant name: {answers.AssistantName}");
        builder.AppendLine($"- Response style: {answers.ResponseStyle}");
        builder.AppendLine();
        builder.AppendLine("## DevCraft Defaults");
        builder.AppendLine();
        builder.AppendLine($"- Default terminal AI agent: {answers.DefaultTerminalAgent}");

        return builder.ToString();
    }
}

