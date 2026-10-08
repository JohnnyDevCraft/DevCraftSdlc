using System.Text.Json;

namespace DevCraft.Cli;

public static class ProjectDevCraftConfigurationWriter
{
    public static string WriteIfMissing(string controlDirectory, ProjectProfile profile)
    {
        string path = Path.Combine(controlDirectory, "configure.json");

        if (File.Exists(path))
        {
            return path;
        }

        ProjectDevCraftConfiguration configuration = new(
            Guid.NewGuid().ToString(),
            "repo-central",
            "features.json",
            profile,
            []);
        string json = JsonSerializer.Serialize(
            configuration,
            new JsonSerializerOptions
            {
                WriteIndented = true,
            });

        File.WriteAllText(path, json);

        return path;
    }
}
