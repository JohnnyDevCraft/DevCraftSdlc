namespace DevCraft.Cli;

public static class DevCraftCli
{
    public static void Run(string[] args)
    {
        StartupContext context = StartupContextResolver.Resolve();

        if (args.Length > 0 && args[0].Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            ListCommand.Run(context, args.Length > 1 ? args[1] : null);
            return;
        }

        if (args.Length > 0 && args[0].Equals("feature", StringComparison.OrdinalIgnoreCase))
        {
            FeatureCommand.Run(
                context,
                args.Skip(1).ToList(),
                new SpectreConsoleInteraction(),
                new FeatureAiSessionLauncher());
            return;
        }

        if (args.Length > 0 && args[0].Equals("merge", StringComparison.OrdinalIgnoreCase))
        {
            CatalogMergeCommand.Run(context, args.Skip(1).ToList());
            return;
        }

        IConsoleInteraction console = new SpectreConsoleInteraction();
        FeatureAiSessionLauncher sessionLauncher = new();
        StartupFlow startupFlow = new(
            console,
            new TerminalAiProjectScanner(),
            () => TerminalAgentCatalog.Create(CommandLocator.Exists));

        startupFlow.Run(context);
        DevCraftMenuCommand.Run(context, console, sessionLauncher, sessionLauncher);
    }
}
