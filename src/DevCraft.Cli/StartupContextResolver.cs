namespace DevCraft.Cli;

public static class StartupContextResolver
{
    public static StartupContext Resolve()
    {
        string currentDirectory = Environment.CurrentDirectory;
        string profileRoot = Environment.GetEnvironmentVariable("DEVCRAFT_PROFILE_HOME")
            ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string profileDirectory = Path.Combine(profileRoot, ".DevCraft");
        string soulFilePath = Path.Combine(profileDirectory, "soul.md");

        return new StartupContext(currentDirectory, profileDirectory, soulFilePath);
    }
}

