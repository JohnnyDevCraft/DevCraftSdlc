using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace DevCraft.Cli;

internal static class TerminalGuiHeader
{
    public static int AddTo(Window window)
    {
        string[] lines = CliLogoRenderer.Create()
            .ToPlainText()
            .ReplaceLineEndings("\n")
            .Split('\n');

        for (int index = 0; index < lines.Length; index++)
        {
            window.Add(new Label
            {
                Text = lines[index],
                X = 0,
                Y = index,
                Width = Dim.Fill(),
            });
        }

        return lines.Length + 1;
    }
}
