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

        DevCraftProfileConfiguration? configuration = JsonSerializer.Deserialize<DevCraftProfileConfiguration>(
            File.ReadAllText(path),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

        return configuration ?? new DevCraftProfileConfiguration([], [], [], [], [], [], "repo-central", []);
    }
}
