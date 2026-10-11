using System.Reflection;

namespace DevCraft.Cli;

public static class DevCraftCurrentVersionProvider
{
    public static string Get()
    {
        string version = typeof(DevCraftCurrentVersionProvider)
            .Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? typeof(DevCraftCurrentVersionProvider).Assembly.GetName().Version?.ToString()
            ?? "unknown";

        int metadataIndex = version.IndexOf('+', StringComparison.Ordinal);

        return metadataIndex >= 0 ? version[..metadataIndex] : version;
    }
}
