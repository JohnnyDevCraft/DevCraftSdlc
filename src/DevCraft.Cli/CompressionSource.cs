namespace DevCraft.Cli;

public sealed record CompressionSource(
    bool IncludeLogEntries,
    IReadOnlyList<string> SummaryTypes);
