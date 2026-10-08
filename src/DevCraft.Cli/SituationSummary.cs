namespace DevCraft.Cli;

public sealed record SituationSummary(
    string RowId,
    DateTimeOffset DateTime,
    string SummaryData,
    string Type,
    bool IsCompressed);
