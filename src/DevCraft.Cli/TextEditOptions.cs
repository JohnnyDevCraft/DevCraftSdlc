namespace DevCraft.Cli;

public sealed record TextEditOptions(
    string Title,
    string InitialText,
    string FileExtension = ".md",
    bool PreferMarkdownHighlighting = true);
