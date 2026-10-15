namespace DevCraft.Cli;

// Branch-name template and operator-defined template variables stored in the profile configure.json.
public sealed record GitToolingSettings
{
    public string BranchTemplate { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> BranchVariables { get; init; } = new Dictionary<string, string>();
}
