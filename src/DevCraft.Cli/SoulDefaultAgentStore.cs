namespace DevCraft.Cli;

public static class SoulDefaultAgentStore
{
    private const string Prefix = SoulAnswersStore.DefaultTerminalAgentPrefix;

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
        SoulAnswersStore.ReplaceOrAppend(lines, Prefix, defaultAgent);

        File.WriteAllText(soulFilePath, string.Join(Environment.NewLine, lines) + Environment.NewLine);
    }
}
