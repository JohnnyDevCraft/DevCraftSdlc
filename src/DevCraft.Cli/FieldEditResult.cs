namespace DevCraft.Cli;

public sealed record FieldEditResult(bool Saved, IReadOnlyDictionary<string, string> Values);
