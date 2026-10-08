namespace DevCraft.Cli;

public static class SupportedTerminalClientResolver
{
    public static SupportedTerminalClient Resolve(string requestedClient, IReadOnlyList<SupportedTerminalClient> clients)
    {
        if (clients.Count == 0)
        {
            return SupportedTerminalClientCatalog.Create()[0];
        }

        return clients.FirstOrDefault(client => Matches(client, requestedClient))
            ?? SupportedTerminalClientCatalog.Create().FirstOrDefault(client => Matches(client, requestedClient))
            ?? clients[0];
    }

    public static bool Matches(SupportedTerminalClient client, string requestedClient)
    {
        string normalized = Normalize(requestedClient);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        return Normalize(client.Slug) == normalized ||
            Normalize(client.Name) == normalized ||
            Normalize(client.Scan.BinaryPath) == normalized ||
            LegacyAliases(client).Any(alias => Normalize(alias) == normalized);
    }

    private static IReadOnlyList<string> LegacyAliases(SupportedTerminalClient client)
    {
        return client.Slug.Equals("claude-code", StringComparison.OrdinalIgnoreCase)
            ? ["Claude AI", "Claude"]
            : [];
    }

    private static string Normalize(string value)
    {
        return value.Trim().Replace(" ", "-", StringComparison.Ordinal).ToLowerInvariant();
    }
}
