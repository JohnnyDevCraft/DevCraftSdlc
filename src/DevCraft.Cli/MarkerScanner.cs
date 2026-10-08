namespace DevCraft.Cli;

public static class MarkerScanner
{
    public static IReadOnlyList<MarkerDetection> Scan(string directoryPath)
    {
        List<MarkerDetection> detections = [];

        foreach (MarkerDefinition definition in MarkerCatalog.Definitions)
        {
            List<string> matchedPaths = definition.Paths
                .Where(path => Exists(directoryPath, path))
                .ToList();

            if (matchedPaths.Count == 0)
            {
                continue;
            }

            detections.Add(new MarkerDetection(definition.Name, definition.Category, matchedPaths));
        }

        return detections;
    }

    private static bool Exists(string root, string relativePath)
    {
        string path = Path.Combine(root, relativePath);

        return File.Exists(path) || Directory.Exists(path);
    }
}

