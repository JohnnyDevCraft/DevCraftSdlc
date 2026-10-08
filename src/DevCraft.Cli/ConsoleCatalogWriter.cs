using Spectre.Console;

namespace DevCraft.Cli;

public static class ConsoleCatalogWriter
{
    public static void Write(DevCraftProfileConfiguration configuration, CatalogCategory category)
    {
        if (category is CatalogCategory.All or CatalogCategory.Skills)
        {
            WriteCategory("Skills", configuration.Skills);
        }

        if (category is CatalogCategory.All or CatalogCategory.Standards)
        {
            WriteCategory("Standards", configuration.Standards);
        }

        if (category is CatalogCategory.All or CatalogCategory.Architectures)
        {
            WriteCategory("Architectures", configuration.Architectures);
        }

        if (category is CatalogCategory.All or CatalogCategory.Templates)
        {
            WriteCategory("Templates", configuration.Templates);
        }

        if (category is CatalogCategory.All or CatalogCategory.ProjectTypes)
        {
            WriteCategory("Project Types", configuration.ProjectTypes);
        }

        if (category is CatalogCategory.All or CatalogCategory.FeatureStorageTypes)
        {
            WriteCategory("Feature Storage Types", configuration.FeatureStorageTypes);
        }

        if (category is CatalogCategory.All or CatalogCategory.SupportedClients)
        {
            WriteClients(configuration.SupportedClients);
        }
    }

    private static void WriteCategory(string title, IReadOnlyList<ProfileCatalogDocument> documents)
    {
        AnsiConsole.MarkupLine($"[green]{Markup.Escape(title)}[/]");

        if (documents.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]  None configured.[/]");
            return;
        }

        Table table = new();
        table.AddColumn("Slug");
        table.AddColumn("Name");
        table.AddColumn("Description");

        foreach (ProfileCatalogDocument document in documents)
        {
            table.AddRow(
                Markup.Escape(document.Slug),
                Markup.Escape(document.Name),
                Markup.Escape(document.Description));
        }

        AnsiConsole.Write(table);
    }

    private static void WriteClients(IReadOnlyList<SupportedTerminalClient> clients)
    {
        AnsiConsole.MarkupLine("[green]Supported Clients[/]");

        if (clients.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]  None configured.[/]");
            return;
        }

        Table table = new();
        table.AddColumn("Slug");
        table.AddColumn("Name");
        table.AddColumn("Description");
        table.AddColumn("Binary");

        foreach (SupportedTerminalClient client in clients)
        {
            table.AddRow(
                Markup.Escape(client.Slug),
                Markup.Escape(client.Name),
                Markup.Escape(client.Description),
                Markup.Escape(client.Scan.BinaryPath));
        }

        AnsiConsole.Write(table);
    }
}
