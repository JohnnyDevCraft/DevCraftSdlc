namespace DevCraft.Cli;

// Defense in depth: a repository's .devcraft folder never holds the profile's soul.md or initialized.md.
public static class ProfileFolderGuard
{
    public static void ThrowIfProfile(string controlDirectory)
    {
        if (File.Exists(Path.Combine(controlDirectory, "soul.md")) || File.Exists(Path.Combine(controlDirectory, "initialized.md")))
        {
            throw new InvalidOperationException(
                $"Refusing to write a project configuration into the DevCraft profile folder ({controlDirectory}). Run devcraft from a project folder.");
        }
    }
}
