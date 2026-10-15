namespace DevCraft.Cli;

public static class CommitMessagePromptBuilder
{
    public const int MaxDiffLength = 12_000;

    public static string Build(string repositoryDirectory, string branch, string stat, string diff)
    {
        bool truncated = diff.Length > MaxDiffLength;
        string shownDiff = truncated ? diff[..MaxDiffLength] : diff;

        return $$"""
            Write a git commit message for the staged changes on branch {{branch}} in {{repositoryDirectory}}.
            Do not run commands and do not modify files. DevCraft runs git itself.

            Return only a JSON object:
            { "success": true, "summary": "<one sentence describing the change>", "message": "<commit message>" }

            The message has a concise imperative subject line of at most 72 characters, then a blank line and a short body explaining why, when the change needs one. Do not add trailers.
            If you cannot write a message, return "success": false and explain why in "summary".

            Staged files:
            {{stat.TrimEnd()}}

            Staged diff{{(truncated ? $" (truncated to the first {MaxDiffLength} characters)" : string.Empty)}}:
            {{shownDiff.TrimEnd()}}
            """;
    }
}
