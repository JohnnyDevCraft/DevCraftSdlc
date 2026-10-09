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
    public void BuildIncludesFileReadingGuidanceWithoutSerializedSituationPayload()
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
        Assert.Contains(Path.Combine(root.Path, "situation"), context);
        Assert.Contains("people.json", context);
        Assert.Contains("log-entries.json", context);
        Assert.Contains("summaries.json", context);
        Assert.Contains("relationship/contact records", context);
        Assert.Contains("JobTitle", context);
        Assert.Contains("AssignedTeam", context);
        Assert.Contains("Organization", context);
        Assert.Contains("InactiveDate", context);
        Assert.Contains("individual entries", context);
        Assert.Contains("IsCompressed=false", context);
        Assert.Contains("week", context);
        Assert.Contains("sprint", context);
        Assert.Contains("month", context);
        Assert.Contains("quarter", context);
        Assert.Contains("year", context);
        Assert.DoesNotContain("Ada", context);
        Assert.DoesNotContain("Active context.", context);
        Assert.DoesNotContain("Week context.", context);
        Assert.DoesNotContain("Archived context.", context);
        Assert.DoesNotContain("Old context.", context);
    }

    [Fact]
    public void BuildIncludesDatabaseGuidanceWithoutExposingConnectionString()
    {
        using TestDirectory root = new();
        const string connectionString = "mongodb://user:secret@localhost:27017/DevCraft";
        DevCraftProfileConfigurationWriter.Write(
            root.Path,
            new DevCraftProfileConfiguration([], [], [], [], [], [], [], true, SituationScale.Sprint, SituationStorage.Database, connectionString));

        string context = SituationPromptContextBuilder.Build(root.Path);

        Assert.Contains(Path.Combine(root.Path, "configure.json"), context);
        Assert.Contains("SituationConnection", context);
        Assert.Contains("DevCraft", context);
        Assert.Contains("People", context);
        Assert.Contains("JobTitle", context);
        Assert.Contains("AssignedTeam", context);
        Assert.Contains("Organization", context);
        Assert.Contains("InactiveDate", context);
        Assert.Contains("LogEntries", context);
        Assert.Contains("Summaries", context);
        Assert.Contains("IsCompressed=false", context);
        Assert.Contains("read-only", context);
        Assert.Contains("mongosh", context);
        Assert.Contains("report the missing MongoDB access", context);
        Assert.DoesNotContain(connectionString, context);
        Assert.DoesNotContain("secret", context);
        Assert.False(Directory.Exists(Path.Combine(root.Path, "situation", "handoff-snapshot")));
    }
}
