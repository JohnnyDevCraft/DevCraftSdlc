using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class SituationPromptContextBuilderTests
{
    [Fact]
    public void BuildOmitsSituationContextWhenDisabled()
    {
        using TestDirectory root = new();
        DevCraftProfileConfigurationWriter.Write(root.Path, new DevCraftProfileConfiguration([], [], [], [], [], [], []));

        string context = SituationPromptContextBuilder.Build(root.Path);

        Assert.Equal(string.Empty, context);
    }

    [Fact]
    public void BuildIncludesUncompressedSituationContextWhenEnabled()
    {
        using TestDirectory root = new();
        DevCraftProfileConfigurationWriter.Write(root.Path, new DevCraftProfileConfiguration([], [], [], [], [], [], [], true));
        FileSituationStore store = new(root.Path);
        store.AddPerson(new SituationPerson("person-1", "Ada", "Lovelace", "ada@example.com", "555-0100", "Advisor", "Active"));
        store.AddLogEntry(new SituationLogEntry("log-1", DateTimeOffset.Parse("2026-10-08T12:00:00-04:00"), "Active context.", false));
        store.AddLogEntry(new SituationLogEntry("log-2", DateTimeOffset.Parse("2026-10-08T13:00:00-04:00"), "Archived context.", true));
        store.AddSummary(new SituationSummary("summary-1", DateTimeOffset.Parse("2026-10-08T14:00:00-04:00"), "Week context.", "week", false));
        store.AddSummary(new SituationSummary("summary-2", DateTimeOffset.Parse("2026-10-08T15:00:00-04:00"), "Old context.", "week", true));

        string context = SituationPromptContextBuilder.Build(root.Path);

        Assert.Contains("Situational Awareness", context);
        Assert.Contains("Ada", context);
        Assert.Contains("Active context.", context);
        Assert.Contains("Week context.", context);
        Assert.DoesNotContain("Archived context.", context);
        Assert.DoesNotContain("Old context.", context);
    }
}
