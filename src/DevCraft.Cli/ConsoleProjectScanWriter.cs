using Spectre.Console;

namespace DevCraft.Cli;

public static class ConsoleProjectScanWriter
{
    public static void Write(ProjectScanResult result)
    {
        if (result.Projects.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No projects detected.[/]");
        }
        else
        {
            Table table = new();
            table.AddColumn("Project");
            table.AddColumn("Type");

            foreach (DetectedProject project in result.Projects)
            {
                table.AddRow(
                    Markup.Escape(project.Name),
                    Markup.Escape(project.Type));
            }

            AnsiConsole.Write(table);
        }

        string sdlcStatus = result.AiDrivenSdlc.Detected
            ? $"AI-driven SDLC detected: {result.AiDrivenSdlc.Name}"
            : "No AI-driven SDLC detected.";

        AnsiConsole.MarkupLine($"[grey]{Markup.Escape(sdlcStatus)}[/]");
    }
}

