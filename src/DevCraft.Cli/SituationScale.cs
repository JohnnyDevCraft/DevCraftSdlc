namespace DevCraft.Cli;

public static class SituationScale
{
    public const string Sprint = "sprint";
    public const string Weeks = "weeks";

    public static string Normalize(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            Sprint => Sprint,
            Weeks => Weeks,
            _ => Weeks,
        };
    }
}
