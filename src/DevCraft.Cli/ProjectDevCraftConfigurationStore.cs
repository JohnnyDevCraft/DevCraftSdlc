namespace DevCraft.Cli;

using System.Text.Json;

public static class ProjectDevCraftConfigurationStore
{
    public static string PathFor(string projectDirectory)
    {
        return Path.Combine(projectDirectory, ".devcraft", "configure.json");
    }

    public static void Write(string projectDirectory, ProjectDevCraftConfiguration configuration)
    {
        string path = PathFor(projectDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        string json = JsonSerializer.Serialize(
            configuration,
            new JsonSerializerOptions
            {
                WriteIndented = true,
            });

        File.WriteAllText(path, json);
    }
}

