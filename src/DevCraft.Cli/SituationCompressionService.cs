using System.Text.Json;

namespace DevCraft.Cli;

public sealed class SituationCompressionService
{
    private readonly ISituationStore store;
    private readonly ISituationSummaryGenerator generator;

    public SituationCompressionService(ISituationStore store, ISituationSummaryGenerator generator)
    {
        this.store = store;
        this.generator = generator;
    }

    public string Compress(
        StartupContext context,
        SupportedTerminalClient client,
        DevCraftProfileConfiguration configuration,
        string targetType)
    {
        CompressionSource source = SourceFor(targetType, configuration.SituationScale);
        SituationSnapshot snapshot = store.Read(true);
        IReadOnlyList<SituationLogEntry> sourceLogs = source.IncludeLogEntries ? snapshot.LogEntries : [];
        IReadOnlyList<SituationSummary> sourceSummaries = snapshot.Summaries
            .Where(summary => source.SummaryTypes.Contains(summary.Type, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (sourceLogs.Count == 0 && sourceSummaries.Count == 0)
        {
            return $"There is no uncompressed situation data to compress into a {targetType} summary yet.";
        }

        string prompt = BuildPrompt(targetType, snapshot.People, sourceLogs, sourceSummaries);
        string response = generator.Generate(context, client, prompt);
        string summaryData = SituationSummaryParser.Parse(response);

        SituationSummary summary = new(
            Guid.NewGuid().ToString("N"),
            DateTimeOffset.Now,
            summaryData,
            targetType,
            false);

        store.AddSummary(summary);
        store.CompleteCompression(
            sourceLogs.Select(entry => entry.RowId).ToList(),
            sourceSummaries.Select(summary => summary.RowId).ToList());

        return $"Created {targetType} situation summary.";
    }

    private static CompressionSource SourceFor(string targetType, string scale)
    {
        return targetType switch
        {
            "week" => new CompressionSource(true, []),
            "sprint" => new CompressionSource(true, ["week"]),
            "month" => new CompressionSource(false, ["week"]),
            "quarter" when scale == SituationScale.Sprint => new CompressionSource(false, ["sprint"]),
            "quarter" => new CompressionSource(false, ["month"]),
            "year" => new CompressionSource(false, ["quarter"]),
            _ => throw new InvalidOperationException($"Unsupported situation summary type: {targetType}."),
        };
    }

    private static string BuildPrompt(
        string targetType,
        IReadOnlyList<SituationPerson> people,
        IReadOnlyList<SituationLogEntry> logEntries,
        IReadOnlyList<SituationSummary> summaries)
    {
        string peopleJson = JsonSerializer.Serialize(people, new JsonSerializerOptions { WriteIndented = true });
        string logJson = JsonSerializer.Serialize(logEntries, new JsonSerializerOptions { WriteIndented = true });
        string summaryJson = JsonSerializer.Serialize(summaries, new JsonSerializerOptions { WriteIndented = true });

        return string.Join(
            Environment.NewLine,
            [
                $"Create a concise DevCraft situational-awareness {targetType} summary from the provided structured records.",
                string.Empty,
                "Return only JSON in this exact shape:",
                "{",
                "  \"summaryData\": \"Concise summary text\"",
                "}",
                string.Empty,
                "People:",
                peopleJson,
                string.Empty,
                "Log entries:",
                logJson,
                string.Empty,
                "Existing lower-level summaries:",
                summaryJson,
            ]);
    }
}
