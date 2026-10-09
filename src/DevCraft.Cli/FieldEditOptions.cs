namespace DevCraft.Cli;

public sealed record FieldEditOptions(string Title, IReadOnlyList<FieldEditField> Fields);

public sealed record FieldEditField(string Key, string Label, string InitialValue = "", bool Required = false);
