namespace DevCraft.Cli;

// Shortest unique display names for a project's working copies. A copy is named by its folder unless that
// folder collides with another copy or just repeats the repository name; then parent folders are used:
// the parent alone, then parent/folder, then more parents until the name is unique.
public static class WorkingCopyNames
{
    public static IReadOnlyList<string> Shortest(IReadOnlyList<string> paths, string repositoryName)
    {
        List<string[]> segments = paths
            .Select(path => path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries))
            .ToList();
        int maxDepth = segments.Count == 0 ? 0 : segments.Max(parts => parts.Length);
        string[] names = new string[paths.Count];

        for (int level = 0; level <= maxDepth; level++)
        {
            List<string?> candidates = segments.Select(parts => Candidate(parts, level)).ToList();

            for (int index = 0; index < paths.Count; index++)
            {
                string? candidate = candidates[index];
                string[] parts = segments[index];

                if (names[index] is not null || candidate is null)
                {
                    continue;
                }

                bool repeatsRepository = level == 0 && parts[^1].Equals(repositoryName, StringComparison.OrdinalIgnoreCase);
                bool collides = candidates.Where((other, position) => position != index)
                    .Any(other => string.Equals(other, candidate, StringComparison.OrdinalIgnoreCase));

                if (!repeatsRepository && !collides)
                {
                    names[index] = candidate;
                }
            }
        }

        return names.Select((name, index) => name ?? paths[index]).ToList();
    }

    // Level 0 is the folder, level 1 the parent alone, and level n >= 2 the last n segments.
    private static string? Candidate(string[] parts, int level)
    {
        if (parts.Length == 0)
        {
            return null;
        }

        if (level == 0)
        {
            return parts[^1];
        }

        if (level == 1)
        {
            return parts.Length >= 2 ? parts[^2] : null;
        }

        return parts.Length >= level ? string.Join('/', parts[^level..]) : null;
    }
}
