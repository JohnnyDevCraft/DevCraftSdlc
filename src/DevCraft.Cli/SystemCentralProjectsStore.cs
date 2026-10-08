namespace DevCraft.Cli;

using System.Text.Json;

public static class SystemCentralProjectsStore
{
    public static string PathFor(string profileDirectory)
    {
        return Path.Combine(profileDirectory, "features", "projects.json");
    }

    public static SystemCentralProjectsIndex Read(string profileDirectory)
    {
        string path = PathFor(profileDirectory);

        if (!File.Exists(path))
        {
            return new SystemCentralProjectsIndex([]);
        }

        SystemCentralProjectsIndex? index = JsonSerializer.Deserialize<SystemCentralProjectsIndex>(
            File.ReadAllText(path),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

        return index ?? new SystemCentralProjectsIndex([]);
    }

    public static void Write(string profileDirectory, SystemCentralProjectsIndex index)
    {
        string path = PathFor(profileDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        string json = JsonSerializer.Serialize(
            index,
            new JsonSerializerOptions
            {
                WriteIndented = true,
            });

        File.WriteAllText(path, json);
    }
}

