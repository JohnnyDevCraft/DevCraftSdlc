namespace DevCraft.Cli;

public sealed record SituationLogEntry(
    string RowId,
    DateTimeOffset DateTime,
    string LogData,
    bool IsCompressed);
