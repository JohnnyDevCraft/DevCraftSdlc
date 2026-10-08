namespace DevCraft.Cli;

public static class CommandLocator
{
    public static bool Exists(string command)
    {
        string? path = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        string executableName = OperatingSystem.IsWindows() ? $"{command}.exe" : command;

        foreach (string directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = Path.Combine(directory, executableName);

            if (File.Exists(candidate))
            {
                return true;
            }
        }

        return false;
    }
}

