using Spectre.Console;

namespace DevCraft.Cli;

public static class ConsoleLogoWriter
{
    public static void Write(CliLogo logo)
    {
        foreach (CliLogoLine line in logo.Lines)
        {
            string devMarkup = Markup.Escape(line.DevSegment);
            string craftMarkup = Markup.Escape(line.CraftSegment);

            AnsiConsole.MarkupLine($"[green]{devMarkup}[/][purple]{craftMarkup}[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[grey]{Markup.Escape(logo.CopyrightLine)}[/]");
        AnsiConsole.MarkupLine($"[grey]{Markup.Escape(logo.CreatorLine)}[/]");
        AnsiConsole.MarkupLine($"[grey]{Markup.Escape(logo.VersionLine)}[/]");
    }
}
