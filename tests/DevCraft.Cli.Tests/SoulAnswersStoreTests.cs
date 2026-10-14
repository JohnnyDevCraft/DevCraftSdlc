using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class SoulAnswersStoreTests
{
    private static readonly SoulSetupAnswers SampleAnswers = new("John", "I build software.", "Jarvis", "Be concise.", "codex");

    [Fact]
    public void SoulAnswersStoreReadsAllFiveAnswers()
    {
        using TestDirectory root = new();
        string soul = Path.Combine(root.Path, "soul.md");
        SoulFileWriter.Write(soul, SampleAnswers);

        Assert.Equal(SampleAnswers, SoulAnswersStore.Read(soul));
    }

    [Fact]
    public void SoulAnswersStoreReadsMissingLabelsAsEmpty()
    {
        using TestDirectory root = new();
        string soul = Path.Combine(root.Path, "soul.md");
        File.WriteAllText(soul, "# SOUL.md\n\n- Name: John\n");

        SoulSetupAnswers answers = SoulAnswersStore.Read(soul);

        Assert.Equal(new SoulSetupAnswers("John", string.Empty, string.Empty, string.Empty, string.Empty), answers);
    }

    [Fact]
    public void SoulAnswersStoreSavePreservesUnknownContent()
    {
        using TestDirectory root = new();
        string soul = Path.Combine(root.Path, "soul.md");
        File.WriteAllText(soul, SoulFileWriter.BuildContent(SampleAnswers) + "\n## Notes\n\nHand-written line.\n");

        SoulAnswersStore.Save(soul, SampleAnswers with { AssistantName = "Friday" });

        string content = File.ReadAllText(soul);
        Assert.Contains("- Assistant name: Friday", content);
        Assert.DoesNotContain("Jarvis", content);
        Assert.Contains("## Notes", content);
        Assert.Contains("Hand-written line.", content);
        Assert.Contains("- Name: John", content);
    }

    [Fact]
    public void SoulAnswersStoreSaveAppendsMissingLabels()
    {
        using TestDirectory root = new();
        string soul = Path.Combine(root.Path, "soul.md");
        File.WriteAllText(soul, "# SOUL.md\n\n- Name: John\n");

        SoulAnswersStore.Save(soul, SampleAnswers);

        Assert.Equal(SampleAnswers, SoulAnswersStore.Read(soul));
        Assert.StartsWith("# SOUL.md", File.ReadAllText(soul));
    }

    [Fact]
    public void SoulAnswersStoreSaveCollapsesNewlines()
    {
        using TestDirectory root = new();
        string soul = Path.Combine(root.Path, "soul.md");
        SoulFileWriter.Write(soul, SampleAnswers);

        SoulAnswersStore.Save(soul, SampleAnswers with { ResponseStyle = "Be concise.\r\n\nAnd witty." });

        Assert.Equal("Be concise. And witty.", SoulAnswersStore.Read(soul).ResponseStyle);
    }
}
