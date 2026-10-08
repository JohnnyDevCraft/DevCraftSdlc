using Spectre.Console;

namespace DevCraft.Cli;

public static class ConsoleDevCraftInstallationWriter
{
    public static void Write(DevCraftInstallationResult result)
    {
        AnsiConsole.MarkupLine($"[green]DevCraft installed: {Markup.Escape(result.ControlDirectory)}[/]");

        foreach (string path in result.CreatedPaths)
        {
            AnsiConsole.MarkupLine($"[grey]  created: {Markup.Escape(path)}[/]");
        }

        foreach (string path in result.PreservedPaths)
        {
            AnsiConsole.MarkupLine($"[grey]  preserved: {Markup.Escape(path)}[/]");
        }
    }
}
