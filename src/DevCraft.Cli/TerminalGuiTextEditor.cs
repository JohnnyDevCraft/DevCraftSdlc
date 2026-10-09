using Terminal.Gui.App;
using Terminal.Gui.Editor;
using Terminal.Gui.Editor.Document;
using Terminal.Gui.Editor.Highlighting;
using Terminal.Gui.Input;
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

        Label hint = new()
        {
            Text = "Ctrl+S Save | Esc Cancel | Ctrl+Z Undo | Ctrl+Y Redo | Ctrl+V Paste",
            X = 0,
            Y = 0,
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

        window.Add(hint, editor, save, cancel);
        app.Run(window);

        return new TextEditResult(saved, editedText);
    }

    private static Editor CreateEditor(TextEditOptions options)
    {
        Editor editor = new()
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
            Document = new TextDocument(options.InitialText),
            WordWrap = true,
            ViewportSettings = ViewportSettingsFlags.HasScrollBars,
            GutterOptions = GutterOptions.LineNumbers,
        };

        if (options.PreferMarkdownHighlighting)
        {
            editor.HighlightingDefinition = HighlightingManager.Instance.GetDefinitionByExtension(options.FileExtension);
        }

        return editor;
    }
}

internal sealed class TerminalGuiTextEditorWindow : Window
{
    private readonly IApplication app;
    private readonly Action save;

    public TerminalGuiTextEditorWindow(IApplication app, string title, Action save)
    {
        this.app = app;
        this.save = save;

        Title = title;
        Width = Dim.Fill();
        Height = Dim.Fill();

        KeyBindings.Add(Key.S.WithCtrl, Command.Save);
        AddCommand(Command.Save, () =>
        {
            SaveAndStop();

            return true;
        });

        KeyBindings.Add(Key.Esc, Command.Quit);
        AddCommand(Command.Quit, () =>
        {
            CancelAndStop();

            return true;
        });
    }

    public void SaveAndStop()
    {
        save();
        app.RequestStop(this);
    }

    public void CancelAndStop() => app.RequestStop(this);
}
