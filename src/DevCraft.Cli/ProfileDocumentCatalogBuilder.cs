namespace DevCraft.Cli;

public static class ProfileDocumentCatalogBuilder
{
    public static IReadOnlyList<ProfileCatalogDocument> Build(string profileDirectory, string folderName)
    {
        string folderPath = Path.Combine(profileDirectory, folderName);

        if (!Directory.Exists(folderPath))
        {
            return [];
        }

        Dictionary<string, int> slugCounts = new(StringComparer.OrdinalIgnoreCase);
        List<ProfileCatalogDocument> documents = [];

        foreach (string path in Directory.EnumerateFiles(folderPath, "*.md").OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            if (ShouldSkip(path))
            {
                continue;
            }

            string name = NameFromFile(path, folderName);
            string slug = UniqueSlug(SlugGenerator.Create(name), slugCounts);

            documents.Add(new ProfileCatalogDocument(
                slug,
                name,
                DescriptionFromFile(path),
                $"{folderName}/{Path.GetFileName(path)}"));
        }

        return documents;
    }

    private static bool ShouldSkip(string path)
    {
        string fileName = Path.GetFileName(path);

        return fileName.Equals("README.md", StringComparison.OrdinalIgnoreCase) ||
            fileName.StartsWith("_", StringComparison.Ordinal);
    }

    private static string UniqueSlug(string slug, Dictionary<string, int> slugCounts)
    {
        if (!slugCounts.TryGetValue(slug, out int count))
        {
            slugCounts[slug] = 1;
            return slug;
        }

        count++;
        slugCounts[slug] = count;

        return $"{slug}-{count}";
    }

    private static string NameFromFile(string path, string folderName)
    {
        if (folderName.Equals("templates", StringComparison.OrdinalIgnoreCase) ||
            folderName.Equals("feature-storage", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetFileNameWithoutExtension(path).Replace('-', ' ');
        }

        foreach (string line in File.ReadLines(path))
        {
            if (!line.StartsWith("# ", StringComparison.Ordinal))
            {
                continue;
            }

            return line[2..].Replace("Skill:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        }

        return Path.GetFileNameWithoutExtension(path).Replace('-', ' ');
    }

    private static string DescriptionFromFile(string path)
    {
        string[] lines = File.ReadAllLines(path);

        string purpose = ReadSection(lines, "Purpose");
        if (!string.IsNullOrWhiteSpace(purpose))
        {
            return purpose;
        }

        string intent = ReadSection(lines, "Intent");
        if (!string.IsNullOrWhiteSpace(intent))
        {
            return intent;
        }

        return FirstParagraph(lines);
    }

    private static string ReadSection(IReadOnlyList<string> lines, string heading)
    {
        for (int index = 0; index < lines.Count; index++)
        {
            if (!lines[index].Equals($"## {heading}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return FirstParagraph(lines.Skip(index + 1));
        }

        return string.Empty;
    }

    private static string FirstParagraph(IEnumerable<string> lines)
    {
        List<string> paragraph = [];

        foreach (string line in lines)
        {
            string trimmed = line.Trim();

            if (trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                if (paragraph.Count > 0)
                {
                    break;
                }

                continue;
            }

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                if (paragraph.Count > 0)
                {
                    break;
                }

                continue;
            }

            paragraph.Add(trimmed.TrimStart('-', ' '));
        }

        return paragraph.Count == 0
            ? "No description available."
            : string.Join(" ", paragraph);
    }
}
