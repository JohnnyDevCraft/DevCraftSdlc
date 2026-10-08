namespace DevCraft.Cli;

public static class SituationPromptContextBuilder
{
    public static string Build(string profileDirectory)
    {
        DevCraftProfileConfiguration configuration = ProfileConfigurationReader.Read(profileDirectory);

        if (!configuration.SituationEnabled)
        {
            return string.Empty;
        }

        return configuration.SituationStorage == SituationStorage.Database
            ? BuildDatabaseGuidance(profileDirectory, configuration)
            : BuildFileGuidance(profileDirectory, configuration);
    }

    private static string BuildFileGuidance(string profileDirectory, DevCraftProfileConfiguration configuration)
    {
        _ = new FileSituationStore(profileDirectory);
        string situationDirectory = Path.Combine(profileDirectory, "situation");

        return $"""

            Situational Awareness:
            Read the active profile-level situation files when situational awareness is relevant.
            - Storage: file
            - Scale: {configuration.SituationScale}
            - Situation folder: {situationDirectory}
            - {Path.Combine(situationDirectory, "people.json")}: relationship/contact records for tracked people. Read all records.
            - {Path.Combine(situationDirectory, "log-entries.json")}: individual entries. Use only records where IsCompressed=false.
            - {Path.Combine(situationDirectory, "summaries.json")}: summary records. Use only records where IsCompressed=false.
            - Summary Type values:
              - week: daily rollups.
              - sprint: sprint summaries.
              - month: weekly rollups.
              - quarter: month or sprint rollups, depending on the configured scale.
              - year: quarter rollups.
            - Do not paste these records back into the prompt unless the operator explicitly asks; read the files directly as needed.
            """;
    }

    private static string BuildDatabaseGuidance(string profileDirectory, DevCraftProfileConfiguration configuration)
    {
        string configurationPath = Path.Combine(profileDirectory, "configure.json");
        string databaseName = TryReadDatabaseName(configuration.SituationConnection);

        return $"""

            Situational Awareness:
            Read active profile-level situation records from MongoDB when situational awareness is relevant.
            - Storage: database
            - Scale: {configuration.SituationScale}
            - Profile configuration: {configurationPath}
            - Read SituationConnection from the local profile configuration when you need to connect. Do not echo, summarize, or paste the connection string into the conversation.
            - Database name: {databaseName}
            - Collection People: relationship/contact records for tracked people. Read all documents. Fields: RowId, FirstName, LastName, Email, Phone, Relation, Status.
            - Collection LogEntries: individual entries. Read only documents where IsCompressed=false. Fields: RowId, DateTime, LogData, IsCompressed.
            - Collection Summaries: summary records. Read only documents where IsCompressed=false. Fields: RowId, DateTime, SummaryData, Type, IsCompressed.
            - Summary Type values:
              - week: daily rollups.
              - sprint: sprint summaries.
              - month: weekly rollups.
              - quarter: month or sprint rollups, depending on the configured scale.
              - year: quarter rollups.
            - Use read-only MongoDB access when available, such as mongosh or an installed MongoDB driver/library. Do not run mutating commands.
            - If MongoDB tooling or access is unavailable, report the missing MongoDB access/tooling instead of using stale file-mode logs.
            """;
    }

    private static string TryReadDatabaseName(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "DevCraft";
        }

        try
        {
            MongoDB.Driver.MongoUrl url = MongoDB.Driver.MongoUrl.Create(connectionString);
            return string.IsNullOrWhiteSpace(url.DatabaseName) ? "DevCraft" : url.DatabaseName;
        }
        catch (FormatException)
        {
            return "DevCraft";
        }
    }
}
