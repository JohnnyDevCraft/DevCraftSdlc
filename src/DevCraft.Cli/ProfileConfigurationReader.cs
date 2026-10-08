using System.Text.Json;

namespace DevCraft.Cli;

public static class ProfileConfigurationReader
{
    public static DevCraftProfileConfiguration Read(string profileDirectory)
    {
        string path = Path.Combine(profileDirectory, "configure.json");

        if (!File.Exists(path))
        {
            return new DevCraftProfileConfiguration([], [], [], [], [], [], []);
        }

        return ReadFile(path);
    }

    public static DevCraftProfileConfiguration ReadFile(string path)
    {
        DevCraftProfileConfiguration? configuration = JsonSerializer.Deserialize<DevCraftProfileConfiguration>(
            File.ReadAllText(path),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

        return configuration is null
            ? new DevCraftProfileConfiguration([], [], [], [], [], [], [])
            : Normalize(configuration);
    }

    private static DevCraftProfileConfiguration Normalize(DevCraftProfileConfiguration configuration)
    {
        return new DevCraftProfileConfiguration(
            configuration.Skills ?? [],
            configuration.Standards ?? [],
            configuration.Architectures ?? [],
            configuration.Templates ?? [],
            configuration.ProjectTypes ?? [],
            configuration.FeatureStorageTypes ?? [],
            configuration.SupportedClients ?? [],
            configuration.SituationEnabled,
            SituationScale.Normalize(configuration.SituationScale),
            SituationStorage.Normalize(configuration.SituationStorage),
            string.IsNullOrWhiteSpace(configuration.SituationConnection)
                ? null
                : configuration.SituationConnection.Trim());
    }
}
