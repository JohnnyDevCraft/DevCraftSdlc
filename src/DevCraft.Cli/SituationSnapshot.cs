namespace DevCraft.Cli;

public sealed record SituationSnapshot(
    IReadOnlyList<SituationPerson> People,
    IReadOnlyList<SituationLogEntry> LogEntries,
    IReadOnlyList<SituationSummary> Summaries);
