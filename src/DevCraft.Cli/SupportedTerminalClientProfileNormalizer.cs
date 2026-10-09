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
        return new SupportedTerminalClient(
            string.IsNullOrWhiteSpace(configured.Slug) ? defaultClient.Slug : configured.Slug.Trim(),
            string.IsNullOrWhiteSpace(configured.Name) ? defaultClient.Name : configured.Name.Trim(),
            string.IsNullOrWhiteSpace(configured.Description) ? defaultClient.Description : configured.Description.Trim(),
            MergeOperation(defaultClient.Scan, configured.Scan),
            MergeOperation(defaultClient.Session, configured.Session));
    }

    private static TerminalClientOperation MergeOperation(TerminalClientOperation defaultOperation, TerminalClientOperation? configuredOperation)
    {
        if (configuredOperation is null)
        {
            return defaultOperation;
        }

        return new TerminalClientOperation(
            string.IsNullOrWhiteSpace(configuredOperation.Description) ? defaultOperation.Description : configuredOperation.Description.Trim(),
            string.IsNullOrWhiteSpace(configuredOperation.BinaryPath) ? defaultOperation.BinaryPath : configuredOperation.BinaryPath.Trim(),
            configuredOperation.Arguments is null || configuredOperation.Arguments.Count == 0
                ? defaultOperation.Arguments
                : configuredOperation.Arguments);
    }
}
