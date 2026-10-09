using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class SituationStorageTests
{
    [Fact]
    public void ProfileConfigurationDefaultsSituationalAwarenessOffFileWeeks()
    {
        using TestDirectory root = new();

        DevCraftProfileConfiguration configuration = ProfileConfigurationReader.Read(root.Path);

        Assert.False(configuration.SituationEnabled);
        Assert.Equal("file", configuration.SituationStorage);
        Assert.Equal("weeks", configuration.SituationScale);
        Assert.Null(configuration.SituationConnection);
    }

    [Fact]
    public void FileStoreCreatesSituationFiles()
    {
        using TestDirectory root = new();

        _ = new FileSituationStore(root.Path);

        Assert.True(File.Exists(Path.Combine(root.Path, "situation", "people.json")));
        Assert.True(File.Exists(Path.Combine(root.Path, "situation", "log-entries.json")));
        Assert.True(File.Exists(Path.Combine(root.Path, "situation", "summaries.json")));
    }

    [Fact]
    public void FileStoreAddsPersonAndLogEntry()
    {
        using TestDirectory root = new();
        FileSituationStore store = new(root.Path);

        store.AddPerson(new SituationPerson("person-1", "Ada", "Lovelace", "ada@example.com", "555-0100", "Advisor", "Active"));
        store.AddLogEntry(new SituationLogEntry("log-1", DateTimeOffset.Parse("2026-10-08T12:00:00-04:00"), "Met with Ada.", false));

        SituationSnapshot snapshot = store.Read(false);
        SituationPerson person = Assert.Single(snapshot.People);
        SituationLogEntry entry = Assert.Single(snapshot.LogEntries);
        Assert.Equal("Ada", person.FirstName);
        Assert.Equal("ACTIVE", person.Status);
        Assert.Equal("Met with Ada.", entry.LogData);
    }

    [Fact]
    public void FileStoreReadsLegacyPeopleWithDefaultPositionFields()
    {
        using TestDirectory root = new();
        string situation = Path.Combine(root.Path, "situation");
        Directory.CreateDirectory(situation);
        File.WriteAllText(
            Path.Combine(situation, "people.json"),
            """
            [
              {
                "RowId": "person-1",
                "FirstName": "Ada",
                "LastName": "Lovelace",
                "Email": "ada@example.com",
                "Phone": "555-0100",
                "Relation": "Advisor",
                "Status": "Active"
              }
            ]
            """);

        SituationPerson person = Assert.Single(new FileSituationStore(root.Path).Read(false).People);

        Assert.Equal("Ada", person.FirstName);
        Assert.Equal("", person.JobTitle);
        Assert.Equal("", person.AssignedTeam);
        Assert.Equal("", person.Organization);
        Assert.Null(person.InactiveDate);
    }

    [Fact]
    public void FileStoreUpsertsPersonWithoutChangingRowId()
    {
        using TestDirectory root = new();
        FileSituationStore store = new(root.Path);
        SituationPerson original = new(
            "person-1",
            "Ada",
            "Lovelace",
            "ada@example.com",
            "555-0100",
            "Advisor",
            "ACTIVE",
            "Principal Engineer",
            "Platform",
            "Analytical Engines",
            null);
        store.AddPerson(original);

        store.UpsertPerson(original with { Email = "ada@work.example", JobTitle = "Architect" });

        SituationPerson person = Assert.Single(store.Read(false).People);
        Assert.Equal("person-1", person.RowId);
        Assert.Equal("ada@work.example", person.Email);
        Assert.Equal("Architect", person.JobTitle);
    }

    [Fact]
    public void FileCompressionRemovesSourceRecordsAfterSummary()
    {
        using TestDirectory root = new();
        FileSituationStore store = new(root.Path);
        store.AddLogEntry(new SituationLogEntry("log-1", DateTimeOffset.Parse("2026-10-08T12:00:00-04:00"), "Worked on beta.", false));
        SituationCompressionService service = new(store, new FakeSituationSummaryGenerator());

        string message = service.Compress(
            new StartupContext(root.Path, root.Path, Path.Combine(root.Path, "soul.md")),
            SupportedTerminalClientCatalog.Create()[0],
            new DevCraftProfileConfiguration([], [], [], [], [], [], [], true),
            "week");

        SituationSnapshot snapshot = store.Read(false);
        Assert.Equal("Created week situation summary.", message);
        Assert.Empty(snapshot.LogEntries);
        Assert.Single(snapshot.Summaries);
    }

    [Fact]
    public void CompressionWithoutSourceDataDoesNotCallAi()
    {
        using TestDirectory root = new();
        FileSituationStore store = new(root.Path);
        FakeSituationSummaryGenerator generator = new();
        SituationCompressionService service = new(store, generator);

        string message = service.Compress(
            new StartupContext(root.Path, root.Path, Path.Combine(root.Path, "soul.md")),
            SupportedTerminalClientCatalog.Create()[0],
            new DevCraftProfileConfiguration([], [], [], [], [], [], [], true),
            "week");

        Assert.Contains("no uncompressed situation data", message);
        Assert.Equal(0, generator.CallCount);
    }

    [Fact]
    public void SummaryParserReadsFencedJson()
    {
        string summary = SituationSummaryParser.Parse(
            """
            ```json
            { "summaryData": "Wrapped summary." }
            ```
            """);

        Assert.Equal("Wrapped summary.", summary);
    }
}
