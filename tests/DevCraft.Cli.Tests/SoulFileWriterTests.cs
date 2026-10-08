using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class SoulFileWriterTests
{
    [Fact]
    public void BuildContentIncludesOperatorAssistantAndDefaultAgent()
    {
        SoulSetupAnswers answers = new(
            "John",
            "I build software.",
            "Jarvis",
            "Be concise.",
            "Codex");

        string content = SoulFileWriter.BuildContent(answers);

        Assert.Contains("Name: John", content);
        Assert.Contains("Assistant name: Jarvis", content);
        Assert.Contains("Default terminal AI agent: Codex", content);
    }
}

