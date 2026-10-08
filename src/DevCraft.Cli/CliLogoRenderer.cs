using Figgle;
using Figgle.Fonts;

namespace DevCraft.Cli;

public static class CliLogoRenderer
{
    public static CliLogo Create()
    {
        IReadOnlyList<string> devLines = Render(CliBranding.ProductNamePrefix);
        IReadOnlyList<string> craftLines = Render(CliBranding.ProductNameSuffix);
        int lineCount = Math.Max(devLines.Count, craftLines.Count);

        List<CliLogoLine> lines = [];

        for (int index = 0; index < lineCount; index++)
        {
            string devSegment = GetLine(devLines, index);
            string craftSegment = GetLine(craftLines, index);

            lines.Add(new CliLogoLine(devSegment, craftSegment));
        }

        return new CliLogo(
            lines,
            CliBranding.CopyrightLine,
            CliBranding.CreatorLine);
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
}
