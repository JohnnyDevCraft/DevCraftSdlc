using System.Text.Json;

namespace DevCraft.Cli;

public static class DevCraftProfileConfigurationWriter
{
    public static void Write(string profileDirectory, DevCraftProfileConfiguration configuration)
    {
        Directory.CreateDirectory(profileDirectory);

        string path = Path.Combine(profileDirectory, "configure.json");
        string json = JsonSerializer.Serialize(
            configuration,
            new JsonSerializerOptions
            {
                WriteIndented = true,
            });

        File.WriteAllText(path, json);
    }
}
