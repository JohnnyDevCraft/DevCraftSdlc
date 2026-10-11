namespace DevCraft.Cli;

public static class DevCraftRepositoryVersionStore
{
    public static string PathFor(string projectDirectory)
    {
        return Path.Combine(projectDirectory, ".devcraft", "version");
    }

    public static string? Read(string projectDirectory)
    {
        string path = PathFor(projectDirectory);

        return File.Exists(path)
            ? File.ReadAllText(path).Trim()
            : null;
    }

    public static void Write(string projectDirectory, string version)
    {
        string path = PathFor(projectDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, version.Trim() + Environment.NewLine);
    }
}
