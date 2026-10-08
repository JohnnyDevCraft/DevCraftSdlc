namespace DevCraft.Cli;

public static class SoulDefaultAgentStore
{
    private const string Prefix = "- Default terminal AI agent:";

    public static string Read(string soulFilePath)
    {
        if (!File.Exists(soulFilePath))
        {
            return "Codex";
        }

        string line = File
            .ReadLines(soulFilePath)
            .FirstOrDefault(value => value.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(line))
        {
            return "Codex";
        }

        return line.Split(':', 2)[1].Trim();
    }

    public static void Write(string soulFilePath, string defaultAgent)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(soulFilePath) ?? ".");

        if (!File.Exists(soulFilePath))
        {
            File.WriteAllText(soulFilePath, $"{Prefix} {defaultAgent}{Environment.NewLine}");
            return;
        }

        List<string> lines = File.ReadAllLines(soulFilePath).ToList();
        int index = lines.FindIndex(line => line.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase));
        string replacement = $"{Prefix} {defaultAgent}";

        if (index >= 0)
        {
            lines[index] = replacement;
        }
        else
        {
            lines.Add(replacement);
        }

        File.WriteAllText(soulFilePath, string.Join(Environment.NewLine, lines) + Environment.NewLine);
    }
}
