namespace DevCraft.Cli;

// The soul's default terminal AI agent, resolved against the profile's supported clients.
public static class DefaultTerminalClient
{
    public static SupportedTerminalClient Resolve(StartupContext context)
    {
        return SupportedTerminalClientResolver.Resolve(
            SoulDefaultAgentStore.Read(context.SoulFilePath),
            ProfileConfigurationReader.Read(context.ProfileDirectory).SupportedClients);
    }
}
