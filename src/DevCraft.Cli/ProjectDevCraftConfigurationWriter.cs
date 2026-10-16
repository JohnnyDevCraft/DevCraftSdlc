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
            null,
            "features.json",
            profile,
            []);
        File.WriteAllText(path, ProjectDevCraftConfigurationStore.Serialize(configuration));

        return path;
    }
}
