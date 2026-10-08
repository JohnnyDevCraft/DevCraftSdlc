using MongoDB.Driver;

namespace DevCraft.Cli;

public sealed class MongoSituationStore : ISituationStore
{
    private readonly IMongoCollection<SituationPerson> people;
    private readonly IMongoCollection<SituationLogEntry> logEntries;
    private readonly IMongoCollection<SituationSummary> summaries;

    public MongoSituationStore(string connectionString)
    {
        MongoUrl url = MongoUrl.Create(connectionString);
        MongoClient client = new(connectionString);
        IMongoDatabase database = client.GetDatabase(string.IsNullOrWhiteSpace(url.DatabaseName) ? "DevCraft" : url.DatabaseName);
        people = database.GetCollection<SituationPerson>("People");
        logEntries = database.GetCollection<SituationLogEntry>("LogEntries");
        summaries = database.GetCollection<SituationSummary>("Summaries");
    }

    public SituationSnapshot Read(bool uncompressedOnly)
    {
        IReadOnlyList<SituationPerson> peopleRecords = people.Find(_ => true).ToList();
        IReadOnlyList<SituationLogEntry> entryRecords = uncompressedOnly
            ? logEntries.Find(entry => !entry.IsCompressed).ToList()
            : logEntries.Find(_ => true).ToList();
        IReadOnlyList<SituationSummary> summaryRecords = uncompressedOnly
            ? summaries.Find(summary => !summary.IsCompressed).ToList()
            : summaries.Find(_ => true).ToList();

        return new SituationSnapshot(peopleRecords, entryRecords, summaryRecords);
    }

    public void AddPerson(SituationPerson person)
    {
        people.InsertOne(person);
    }

    public void AddLogEntry(SituationLogEntry logEntry)
    {
        logEntries.InsertOne(logEntry);
    }

    public void AddSummary(SituationSummary summary)
    {
        summaries.InsertOne(summary);
    }

    public void CompleteCompression(IReadOnlyList<string> logEntryIds, IReadOnlyList<string> summaryIds)
    {
        if (logEntryIds.Count > 0)
        {
            logEntries.UpdateMany(
                entry => logEntryIds.Contains(entry.RowId),
                Builders<SituationLogEntry>.Update.Set(entry => entry.IsCompressed, true));
        }

        if (summaryIds.Count > 0)
        {
            summaries.UpdateMany(
                summary => summaryIds.Contains(summary.RowId),
                Builders<SituationSummary>.Update.Set(summary => summary.IsCompressed, true));
        }
    }

    public void Replace(SituationSnapshot snapshot)
    {
        people.DeleteMany(_ => true);
        logEntries.DeleteMany(_ => true);
        summaries.DeleteMany(_ => true);

        if (snapshot.People.Count > 0)
        {
            people.InsertMany(snapshot.People);
        }

        if (snapshot.LogEntries.Count > 0)
        {
            logEntries.InsertMany(snapshot.LogEntries);
        }

        if (snapshot.Summaries.Count > 0)
        {
            summaries.InsertMany(snapshot.Summaries);
        }
    }
}
