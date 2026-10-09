namespace DevCraft.Cli;

public interface ISituationStore
{
    SituationSnapshot Read(bool uncompressedOnly);

    void AddPerson(SituationPerson person);

    void UpsertPerson(SituationPerson person);

    void AddLogEntry(SituationLogEntry logEntry);

    void UpsertLogEntry(SituationLogEntry logEntry);

    void AddSummary(SituationSummary summary);

    void UpsertSummary(SituationSummary summary);

    void CompleteCompression(IReadOnlyList<string> logEntryIds, IReadOnlyList<string> summaryIds);

    void Replace(SituationSnapshot snapshot);
}
