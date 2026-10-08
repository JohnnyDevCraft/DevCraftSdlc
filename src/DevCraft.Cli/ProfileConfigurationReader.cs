using System.Text.Json;

namespace DevCraft.Cli;

public static class ProfileConfigurationReader
{
    public static DevCraftProfileConfiguration Read(string profileDirectory)
    {
        string path = Path.Combine(profileDirectory, "configure.json");

        if (!File.Exists(path))
        {
            return new DevCraftProfileConfiguration([], [], [], [], [], [], "repo-central", []);
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
            ? new DevCraftProfileConfiguration([], [], [], [], [], [], "repo-central", [])
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
            string.IsNullOrWhiteSpace(configuration.SelectedFeatureStorage)
                ? "repo-central"
                : configuration.SelectedFeatureStorage,
            configuration.SupportedClients ?? []);
    }
}
