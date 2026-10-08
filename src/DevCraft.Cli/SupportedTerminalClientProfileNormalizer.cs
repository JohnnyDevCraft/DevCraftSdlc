namespace DevCraft.Cli;

public static class SupportedTerminalClientProfileNormalizer
{
    public static IReadOnlyList<SupportedTerminalClient> Normalize(IReadOnlyList<SupportedTerminalClient> configuredClients)
    {
        IReadOnlyList<SupportedTerminalClient> defaults = SupportedTerminalClientCatalog.Create();

        if (configuredClients.Count == 0)
        {
            return defaults;
        }

        List<SupportedTerminalClient> normalized = [];

        foreach (SupportedTerminalClient defaultClient in defaults)
        {
            SupportedTerminalClient? configured = configuredClients.FirstOrDefault(client => SupportedTerminalClientResolver.Matches(defaultClient, client.Slug) || SupportedTerminalClientResolver.Matches(defaultClient, client.Name));
            normalized.Add(configured is null ? defaultClient : Merge(defaultClient, configured));
        }

        normalized.AddRange(configuredClients.Where(configured => !normalized.Any(client => SupportedTerminalClientResolver.Matches(client, configured.Slug))));

        return normalized;
    }

    private static SupportedTerminalClient Merge(SupportedTerminalClient defaultClient, SupportedTerminalClient configured)
    {
        return defaultClient with
        {
            Scan = defaultClient.Scan with
            {
                BinaryPath = string.IsNullOrWhiteSpace(configured.Scan.BinaryPath)
                    ? defaultClient.Scan.BinaryPath
                    : configured.Scan.BinaryPath,
            },
            Session = defaultClient.Session with
            {
                BinaryPath = string.IsNullOrWhiteSpace(configured.Session.BinaryPath)
                    ? defaultClient.Session.BinaryPath
                    : configured.Session.BinaryPath,
            },
        };
    }
}
