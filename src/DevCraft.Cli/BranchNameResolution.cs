namespace DevCraft.Cli;

public sealed record BranchNameResolution(string BranchName, IReadOnlyList<string> UnresolvedTokens);
