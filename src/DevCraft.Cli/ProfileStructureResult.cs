namespace DevCraft.Cli;

public sealed record ProfileStructureResult(
    IReadOnlyList<string> CreatedDirectories,
    IReadOnlyList<string> CreatedFiles,
    IReadOnlyList<string> UpdatedFiles);
