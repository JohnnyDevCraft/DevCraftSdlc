using DevCraft.Cli;
using Terminal.Gui.Editor;

namespace DevCraft.Cli.Tests;

public sealed class TerminalGuiTextEditorTests
{
    [Fact]
    public void CreateEditorDisablesLineNumbers()
    {
        TextEditOptions options = new("Edit", "line one", PreferMarkdownHighlighting: false, FileExtension: ".md");

        Editor editor = TerminalGuiTextEditor.CreateEditor(options);

        Assert.Equal(GutterOptions.None, editor.GutterOptions);
    }
}
