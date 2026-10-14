using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace DevCraft.Cli;

internal static class TerminalGuiHeader
{
    // The full logo header uses about 15 rows of window chrome; below this height the editors lose their content area.
    internal const int FullHeaderMinimumRows = 30;

    public static int AddTo(Window window, int terminalRows)
    {
        IReadOnlyList<string> lines = Lines(CliLogoRenderer.Create(), ModeFor(terminalRows));

        for (int index = 0; index < lines.Count; index++)
        {
            window.Add(new Label
            {
                Text = lines[index],
                X = 0,
                Y = index,
                Width = Dim.Fill(),
                Height = 1,
            });
        }

        return lines.Count + 1;
    }

    internal static TerminalGuiHeaderMode ModeFor(int terminalRows)
    {
        return terminalRows >= FullHeaderMinimumRows
            ? TerminalGuiHeaderMode.Full
            : TerminalGuiHeaderMode.Compact;
    }

    internal static IReadOnlyList<string> Lines(CliLogo logo, TerminalGuiHeaderMode mode)
    {
        if (mode == TerminalGuiHeaderMode.Compact)
        {
            return [$"{CliBranding.ProductName} | {logo.VersionLine}"];
        }

        return logo
            .ToPlainText()
            .ReplaceLineEndings("\n")
            .Split('\n');
    }
}
