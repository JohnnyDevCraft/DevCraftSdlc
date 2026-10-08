using Figgle;
using Figgle.Fonts;
using System.Reflection;

namespace DevCraft.Cli;

public static class CliLogoRenderer
{
    public static CliLogo Create()
    {
        IReadOnlyList<string> devLines = Render(CliBranding.ProductNamePrefix);
        IReadOnlyList<string> craftLines = Render(CliBranding.ProductNameSuffix);
        int lineCount = Math.Max(devLines.Count, craftLines.Count);
        int devWidth = devLines.Count == 0 ? 0 : devLines.Max(line => line.Length) + 2;

        List<CliLogoLine> lines = [];

        for (int index = 0; index < lineCount; index++)
        {
            string devSegment = GetLine(devLines, index);
            string craftSegment = GetLine(craftLines, index);

            lines.Add(new CliLogoLine(devSegment.PadRight(devWidth), craftSegment));
        }

        return new CliLogo(
            lines,
            CliBranding.CopyrightLine,
            CliBranding.CreatorLine,
            $"Version {GetVersion()}");
    }

    private static IReadOnlyList<string> Render(string text)
    {
        return FiggleFonts.Standard
            .Render(text)
            .TrimEnd()
            .Split(Environment.NewLine);
    }

    private static string GetLine(IReadOnlyList<string> lines, int index)
    {
        return index < lines.Count ? lines[index] : string.Empty;
    }

    private static string GetVersion()
    {
        string version = typeof(CliLogoRenderer)
            .Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? typeof(CliLogoRenderer).Assembly.GetName().Version?.ToString()
            ?? "unknown";

        int metadataIndex = version.IndexOf('+', StringComparison.Ordinal);

        return metadataIndex >= 0 ? version[..metadataIndex] : version;
    }
}
