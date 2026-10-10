using Terminal.Gui.App;
using Terminal.Gui.Editor;
using Terminal.Gui.Editor.Document;
using Terminal.Gui.Editor.Highlighting;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace DevCraft.Cli;

public static class TerminalGuiTextEditor
{
    public static TextEditResult Edit(TextEditOptions options)
    {
        bool saved = false;
        string editedText = options.InitialText;

        using IApplication app = Application.Create();
        app.Init();

        Editor editor = CreateEditor(options);
        using TerminalGuiTextEditorWindow window = new(
            app,
            $"{options.Title} - Ctrl+S Save, Esc Cancel",
            () =>
            {
                saved = true;
                editedText = editor.Document?.Text ?? string.Empty;
            });

        int contentTop = TerminalGuiHeader.AddTo(window);
        Label hint = new()
        {
            Text = "Ctrl+S Save | Esc Cancel | Ctrl+Z Undo | Ctrl+Y Redo | Ctrl+V Paste",
            X = 0,
            Y = contentTop,
            Width = Dim.Fill(),
        };
        Button save = new()
        {
            Text = "Save",
            X = 0,
            Y = Pos.AnchorEnd(1),
            IsDefault = true,
        };
        Button cancel = new()
        {
            Text = "Cancel",
            X = Pos.Right(save) + 2,
            Y = Pos.Top(save),
        };

        save.Accepted += (_, _) => window.SaveAndStop();
        cancel.Accepted += (_, _) => window.CancelAndStop();

        editor.Y = contentTop + 2;
        window.Add(hint, editor, save, cancel);
        app.Run(window);

        return new TextEditResult(saved, editedText);
    }

    internal static Editor CreateEditor(TextEditOptions options)
    {
        Editor editor = new()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
            Document = new TextDocument(options.InitialText),
            WordWrap = true,
            ViewportSettings = ViewportSettingsFlags.HasScrollBars,
            GutterOptions = GutterOptions.None,
        };

        if (options.PreferMarkdownHighlighting)
        {
            editor.HighlightingDefinition = HighlightingManager.Instance.GetDefinitionByExtension(options.FileExtension);
        }

        return editor;
    }
}
