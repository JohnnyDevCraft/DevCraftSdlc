namespace DevCraft.Cli;

// One checkout of a project's repository, as last seen by DevCraft.
public sealed record WorkingCopy(string Path, string? Branch, DateTimeOffset? LastSeen);
