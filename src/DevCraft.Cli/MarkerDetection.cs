namespace DevCraft.Cli;

public sealed record MarkerDetection(
    string Name,
    MarkerCategory Category,
    IReadOnlyList<string> MatchedPaths)
{
    public bool IsDevCraft =>
        Category == MarkerCategory.SdlcWorkflow &&
        Name.Equals("DevCraft", StringComparison.OrdinalIgnoreCase);
}

