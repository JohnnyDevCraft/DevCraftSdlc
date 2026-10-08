namespace DevCraft.Cli;

using System.Text.Json;

public static class ProjectDevCraftConfigurationReader
{
    public static ProjectDevCraftConfiguration? Read(string projectDirectory)
    {
        string path = Path.Combine(projectDirectory, ".devcraft", "configure.json");

        if (!File.Exists(path))
        {
            return null;
        }

        ProjectDevCraftConfiguration? configuration = JsonSerializer.Deserialize<ProjectDevCraftConfiguration>(
            File.ReadAllText(path),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

        if (configuration is null)
        {
            return null;
        }

        return configuration with
        {
            Features = configuration.Features ?? [],
        };
    }
}

