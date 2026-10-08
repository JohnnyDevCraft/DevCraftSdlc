namespace DevCraft.Cli;

public static class ListCommand
{
    public static void Run(StartupContext context, string? category)
    {
        ProfileStructureInitializer.Ensure(context.ProfileDirectory);
        DevCraftProfileConfiguration configuration = ProfileConfigurationReader.Read(context.ProfileDirectory);
        ConsoleCatalogWriter.Write(configuration, CatalogCategoryParser.Parse(category));
    }
}
