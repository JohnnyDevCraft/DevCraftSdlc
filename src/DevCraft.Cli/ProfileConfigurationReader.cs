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
        string json = File.ReadAllText(path);
        int sourceSchemaVersion = ReadSourceSchemaVersion(json, path);
        DevCraftProfileConfiguration? configuration = JsonSerializer.Deserialize<DevCraftProfileConfiguration>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

        return configuration is null
            ? new DevCraftProfileConfiguration([], [], [], [], [], [], [])
            : Migrate(configuration, sourceSchemaVersion, path);
    }

    private static DevCraftProfileConfiguration Migrate(DevCraftProfileConfiguration configuration, int sourceSchemaVersion, string path)
    {
        if (sourceSchemaVersion > DevCraftProfileConfiguration.CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Profile configuration schema version {sourceSchemaVersion} in {path} is newer than this DevCraft version supports. Update DevCraft before modifying the profile configuration.");
        }

        return Normalize(configuration, DevCraftProfileConfiguration.CurrentSchemaVersion);
    }

    private static int ReadSourceSchemaVersion(string json, string path)
    {
        using JsonDocument document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty(nameof(DevCraftProfileConfiguration.SchemaVersion), out JsonElement schemaVersionElement))
        {
            return 0;
        }

        if (!schemaVersionElement.TryGetInt32(out int schemaVersion) || schemaVersion < 0)
        {
            throw new InvalidOperationException($"Profile configuration schema version in {path} is invalid.");
        }

        return schemaVersion;
    }

    private static DevCraftProfileConfiguration Normalize(DevCraftProfileConfiguration configuration, int schemaVersion)
    {
        return new DevCraftProfileConfiguration(
            configuration.Skills ?? [],
            configuration.Standards ?? [],
            configuration.Architectures ?? [],
            configuration.Templates ?? [],
            configuration.ProjectTypes ?? [],
            configuration.FeatureStorageTypes ?? [],
            SupportedTerminalClientProfileNormalizer.Normalize(configuration.SupportedClients ?? []),
            configuration.SituationEnabled,
            SituationScale.Normalize(configuration.SituationScale),
            SituationStorage.Normalize(configuration.SituationStorage),
            string.IsNullOrWhiteSpace(configuration.SituationConnection)
                ? null
                : configuration.SituationConnection.Trim(),
            schemaVersion);
    }
}
