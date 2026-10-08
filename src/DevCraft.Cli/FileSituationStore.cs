using System.Text.Json;

namespace DevCraft.Cli;

public sealed class FileSituationStore : ISituationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string situationDirectory;

    public FileSituationStore(string profileDirectory)
    {
        situationDirectory = Path.Combine(profileDirectory, "situation");
        EnsureFiles();
    }

    public SituationSnapshot Read(bool uncompressedOnly)
    {
        IReadOnlyList<SituationPerson> people = ReadList<SituationPerson>(PeoplePath);
        IReadOnlyList<SituationLogEntry> logEntries = ReadList<SituationLogEntry>(LogEntriesPath);
        IReadOnlyList<SituationSummary> summaries = ReadList<SituationSummary>(SummariesPath);

        if (!uncompressedOnly)
        {
            return new SituationSnapshot(people, logEntries, summaries);
        }

        return new SituationSnapshot(
            people,
            logEntries.Where(entry => !entry.IsCompressed).ToList(),
            summaries.Where(summary => !summary.IsCompressed).ToList());
    }

    public void AddPerson(SituationPerson person)
    {
        List<SituationPerson> people = ReadList<SituationPerson>(PeoplePath).ToList();
        people.Add(person);
        WriteList(PeoplePath, people);
    }

    public void AddLogEntry(SituationLogEntry logEntry)
    {
        List<SituationLogEntry> entries = ReadList<SituationLogEntry>(LogEntriesPath).ToList();
        entries.Add(logEntry);
        WriteList(LogEntriesPath, entries);
    }

    public void AddSummary(SituationSummary summary)
    {
        List<SituationSummary> summaries = ReadList<SituationSummary>(SummariesPath).ToList();
        summaries.Add(summary);
        WriteList(SummariesPath, summaries);
    }

    public void CompleteCompression(IReadOnlyList<string> logEntryIds, IReadOnlyList<string> summaryIds)
    {
        HashSet<string> logIds = logEntryIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> summaryIdSet = summaryIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        WriteList(LogEntriesPath, ReadList<SituationLogEntry>(LogEntriesPath).Where(entry => !logIds.Contains(entry.RowId)).ToList());
        WriteList(SummariesPath, ReadList<SituationSummary>(SummariesPath).Where(summary => !summaryIdSet.Contains(summary.RowId)).ToList());
    }

    public void Replace(SituationSnapshot snapshot)
    {
        EnsureFiles();
        WriteList(PeoplePath, snapshot.People);
        WriteList(LogEntriesPath, snapshot.LogEntries);
        WriteList(SummariesPath, snapshot.Summaries);
    }

    private string PeoplePath => Path.Combine(situationDirectory, "people.json");

    private string LogEntriesPath => Path.Combine(situationDirectory, "log-entries.json");

    private string SummariesPath => Path.Combine(situationDirectory, "summaries.json");

    private void EnsureFiles()
    {
        Directory.CreateDirectory(situationDirectory);
        EnsureFile(PeoplePath);
        EnsureFile(LogEntriesPath);
        EnsureFile(SummariesPath);
    }

    private static void EnsureFile(string path)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, "[]");
        }
    }

    private static IReadOnlyList<T> ReadList<T>(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        return JsonSerializer.Deserialize<IReadOnlyList<T>>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }

    private static void WriteList<T>(string path, IReadOnlyList<T> values)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(values, JsonOptions));
    }
}
