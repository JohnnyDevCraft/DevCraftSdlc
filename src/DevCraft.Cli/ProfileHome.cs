namespace DevCraft.Cli;

// The folder that holds the DevCraft profile (normally ~). On case-insensitive file systems its .devcraft
// is the profile folder itself, so DevCraft must never treat it as a project.
public static class ProfileHome
{
    public static bool Is(StartupContext context)
    {
        string? parent = Path.GetDirectoryName(WorkingCopyPaths.Normalize(context.ProfileDirectory));

        return parent is not null && WorkingCopyPaths.Same(context.CurrentDirectory, parent);
    }

    public static string Notice(StartupContext context)
    {
        return $"This is your home folder, where your DevCraft profile lives ({context.ProfileDirectory}). DevCraft manages projects from their own folders. Open a project folder and run devcraft there.";
    }
}
