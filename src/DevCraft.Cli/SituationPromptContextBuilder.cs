using System.Text.Json;

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

        SituationSnapshot snapshot;

        try
        {
            snapshot = SituationStoreFactory.Create(profileDirectory, configuration).Read(true);
        }
        catch (InvalidOperationException exception)
        {
            return $"""

                Situational Awareness:
                - Enabled: true
                - Status: {exception.Message}
                """;
        }

        string json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });

        return $"""

            Situational Awareness:
            Use this active profile-level situation context when it is relevant. It contains all people and uncompressed log entries and summaries only.

            {json}
            """;
    }
}
