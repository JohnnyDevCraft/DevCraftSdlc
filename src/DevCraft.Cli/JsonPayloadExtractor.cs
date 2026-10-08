namespace DevCraft.Cli;

public static class JsonPayloadExtractor
{
    public static string Extract(string value)
    {
        string trimmed = value.Trim();

        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            trimmed = StripCodeFence(trimmed);
        }

        int objectStart = trimmed.IndexOf('{');
        int arrayStart = trimmed.IndexOf('[');
        int start = FirstJsonStart(objectStart, arrayStart);

        if (start > 0)
        {
            trimmed = trimmed[start..];
        }

        int objectEnd = trimmed.LastIndexOf('}');
        int arrayEnd = trimmed.LastIndexOf(']');
        int end = Math.Max(objectEnd, arrayEnd);

        return end >= 0
            ? trimmed[..(end + 1)]
            : trimmed;
    }

    private static string StripCodeFence(string value)
    {
        string withoutOpeningFence = value;
        int firstNewLine = value.IndexOf('\n');

        if (firstNewLine >= 0)
        {
            withoutOpeningFence = value[(firstNewLine + 1)..];
        }

        int closingFence = withoutOpeningFence.LastIndexOf("```", StringComparison.Ordinal);

        return closingFence >= 0
            ? withoutOpeningFence[..closingFence].Trim()
            : withoutOpeningFence.Trim();
    }

    private static int FirstJsonStart(int objectStart, int arrayStart)
    {
        if (objectStart < 0)
        {
            return arrayStart;
        }

        if (arrayStart < 0)
        {
            return objectStart;
        }

        return Math.Min(objectStart, arrayStart);
    }
}
