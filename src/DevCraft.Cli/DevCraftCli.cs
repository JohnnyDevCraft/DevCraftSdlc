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
        bool forceInstall = args.Length > 0 && args[0].Equals("-force", StringComparison.OrdinalIgnoreCase);

        if (forceInstall && args.Length > 1)
        {
            console.WriteStatus("Unsupported argument combination. Use `devcraft -force` by itself.");
            return;
        }

        if (args.Length > 0 && args[0].Equals("select-agent", StringComparison.OrdinalIgnoreCase))
        {
            AgentSelectionCommand.Run(context, console, CommandLocator.Exists);
        }

        StartupFlow startupFlow = new(
            console,
            new TerminalAiProjectScanner(),
            () => TerminalAgentCatalog.Create(CommandLocator.Exists));

        startupFlow.Run(context, forceInstall);
        DevCraftMenuCommand.Run(context, console, sessionLauncher, sessionLauncher);
    }
}
