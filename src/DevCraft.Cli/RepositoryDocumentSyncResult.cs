namespace DevCraft.Cli;

public sealed record RepositoryDocumentSyncResult(
    bool Success,
    string Summary,
    IReadOnlyList<string> UpdatedFiles,
    IReadOnlyList<string> SkippedFiles)
{
    public static RepositoryDocumentSyncResult Failed(string summary)
    {
        return new RepositoryDocumentSyncResult(false, summary, [], []);
    }
}
