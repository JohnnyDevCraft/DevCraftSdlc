namespace DevCraft.Cli;

public static class ProfileStateDetector
{
    public static ProfileState Detect(StartupContext context)
    {
        bool profileDirectoryExists = Directory.Exists(context.ProfileDirectory);
        bool soulFileExists = profileDirectoryExists && File.Exists(context.SoulFilePath);

        return new ProfileState(
            context.ProfileDirectory,
            context.SoulFilePath,
            profileDirectoryExists,
            soulFileExists);
    }
}

