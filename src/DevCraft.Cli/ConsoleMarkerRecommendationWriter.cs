using Spectre.Console;

namespace DevCraft.Cli;

public static class ConsoleMarkerRecommendationWriter
{
    public static void Write(IReadOnlyList<MarkerDetection> detections)
    {
        IReadOnlyList<MarkerDetection> sdlcDetections = detections
            .Where(detection => detection.Category == MarkerCategory.SdlcWorkflow)
            .ToList();
        IReadOnlyList<MarkerDetection> agentDetections = detections
            .Where(detection => detection.Category == MarkerCategory.AgentConfiguration)
            .ToList();

        WriteSdlcDetections(sdlcDetections);
        WriteAgentDetections(agentDetections);
    }

    private static void WriteSdlcDetections(IReadOnlyList<MarkerDetection> detections)
    {
        if (detections.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]No SDLC workflow markers detected.[/]");
            return;
        }

        foreach (MarkerDetection detection in detections)
        {
            string message = detection.IsDevCraft
                ? "DevCraft SDLC marker detected."
                : $"{detection.Name} SDLC marker detected. This is not DevCraft.";

            AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(message)}[/]");
            WriteEvidence(detection);
        }
    }

    private static void WriteAgentDetections(IReadOnlyList<MarkerDetection> detections)
    {
        if (detections.Count == 0)
        {
            return;
        }

        AnsiConsole.MarkupLine("[grey]Agent configuration markers detected:[/]");

        foreach (MarkerDetection detection in detections)
        {
            AnsiConsole.MarkupLine($"[grey]- {Markup.Escape(detection.Name)}[/]");
            WriteEvidence(detection);
        }
    }

    private static void WriteEvidence(MarkerDetection detection)
    {
        foreach (string path in detection.MatchedPaths)
        {
            AnsiConsole.MarkupLine($"[grey]  evidence: {Markup.Escape(path)}[/]");
        }
    }
}
