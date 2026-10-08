namespace DevCraft.Cli;

public static class SituationStorage
{
    public const string File = "file";
    public const string Database = "database";

    public static string Normalize(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            Database => Database,
            File => File,
            _ => File,
        };
    }
}
