namespace DevCraft.Cli;

internal static class CliHeaderContextCache
{
    private static CliHeaderContext? current;

    public static CliHeaderContext Current
        => current ??= CliHeaderContextProvider.Create(Environment.CurrentDirectory);

    public static void Initialize(string directory)
        => current = CliHeaderContextProvider.Create(directory);

    internal static void SetForTesting(CliHeaderContext headerContext)
        => current = headerContext;
}
