namespace DevCraft.Cli;

// Normalizes git remote URLs so the SSH and HTTPS forms of one repository compare equal:
// user info and scheme removed, host lowercased, trailing "/" and ".git" removed; the path keeps its case.
public static class RemoteUrl
{
    public static string? Normalize(string? url)
    {
        string value = url?.Trim() ?? string.Empty;

        if (value.Length == 0)
        {
            return null;
        }

        int schemeEnd = value.IndexOf("://", StringComparison.Ordinal);

        if (schemeEnd >= 0)
        {
            string rest = value[(schemeEnd + 3)..];
            int pathStart = rest.IndexOf('/');
            string authority = pathStart >= 0 ? rest[..pathStart] : rest;
            string path = pathStart >= 0 ? rest[(pathStart + 1)..] : string.Empty;

            return Join(Host(authority), path);
        }

        int colon = value.IndexOf(':');
        int slash = value.IndexOf('/');
        bool scpLike = colon > 1 && (slash < 0 || colon < slash);

        return scpLike
            ? Join(Host(value[..colon]), value[(colon + 1)..])
            : TrimSuffixes(value);
    }

    public static string? RepositoryName(string? url)
    {
        string? normalized = Normalize(url);

        return normalized?
            .Split(['/', ':', '\\'], StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();
    }

    private static string Host(string authority)
    {
        int at = authority.LastIndexOf('@');

        return (at >= 0 ? authority[(at + 1)..] : authority).ToLowerInvariant();
    }

    private static string Join(string host, string path)
    {
        string trimmedPath = TrimSuffixes(path.TrimStart('/'));

        return trimmedPath.Length == 0 ? host : $"{host}/{trimmedPath}";
    }

    private static string TrimSuffixes(string path)
    {
        string trimmed = path.TrimEnd('/');

        if (trimmed.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^4];
        }

        return trimmed.TrimEnd('/');
    }
}
