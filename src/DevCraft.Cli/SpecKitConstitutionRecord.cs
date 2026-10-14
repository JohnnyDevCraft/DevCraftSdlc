using System.Security.Cryptography;

namespace DevCraft.Cli;

// Records the imported SpecKit constitution and its MD5 hash in the repository .devcraft/README.md.
public static class SpecKitConstitutionRecord
{
    public const string RelativePath = ".specify/memory/constitution.md";
    private const string Heading = "## SpecKit Constitution";
    private const string HashPrefix = "- MD5: `";

    public static string PathFor(string repositoryDirectory)
    {
        return Path.Combine(repositoryDirectory, ".specify", "memory", "constitution.md");
    }

    public static string ComputeMd5(string constitutionPath)
    {
        return Convert.ToHexStringLower(MD5.HashData(File.ReadAllBytes(constitutionPath)));
    }

    public static string? ReadRecordedMd5(string readmePath)
    {
        if (!File.Exists(readmePath))
        {
            return null;
        }

        string[] lines = File.ReadAllText(readmePath).ReplaceLineEndings("\n").Split('\n');
        int heading = Array.FindIndex(lines, line => line.Trim() == Heading);

        if (heading < 0)
        {
            return null;
        }

        string? hashLine = lines
            .Skip(heading + 1)
            .TakeWhile(line => !line.StartsWith("## ", StringComparison.Ordinal))
            .FirstOrDefault(line => line.StartsWith(HashPrefix, StringComparison.Ordinal));

        return hashLine?[HashPrefix.Length..].TrimEnd().TrimEnd('`');
    }

    public static void Record(string readmePath, string md5)
    {
        string existing = File.Exists(readmePath) ? File.ReadAllText(readmePath) : string.Empty;
        string newLine = existing.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        List<string> lines = existing.Length == 0 ? [] : existing.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n').ToList();
        List<string> section =
        [
            Heading,
            string.Empty,
            $"- File: `{RelativePath}`",
            $"{HashPrefix}{md5}`",
        ];
        int heading = lines.FindIndex(line => line.Trim() == Heading);

        if (heading >= 0)
        {
            int end = lines.FindIndex(heading + 1, line => line.StartsWith("## ", StringComparison.Ordinal));
            int count = (end < 0 ? lines.Count : end) - heading;
            lines.RemoveRange(heading, count);

            if (end >= 0)
            {
                section.Add(string.Empty);
            }

            lines.InsertRange(heading, section);
        }
        else
        {
            if (lines.Count > 0)
            {
                lines.Add(string.Empty);
            }

            lines.AddRange(section);
        }

        string updated = string.Join(newLine, lines) + newLine;

        if (updated != existing)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(readmePath)!);
            File.WriteAllText(readmePath, updated);
        }
    }
}
