using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace DevCraft.Cli;

public static class TerminalGuiFieldEditor
{
    public static FieldEditResult Edit(FieldEditOptions options)
    {
        bool saved = false;
        Dictionary<string, TextField> inputs = [];

        using IApplication app = Application.Create();
        app.Init();

        using TerminalGuiFieldEditorWindow window = new(
            app,
            $"{options.Title} - Ctrl+S Save, Esc Cancel",
            () => saved = true);

        int row = 0;

        foreach (FieldEditField field in options.Fields)
        {
            Label label = new()
            {
                Text = field.Required ? $"{field.Label} *" : field.Label,
                X = 0,
                Y = row,
                Width = 24,
            };
            TextField input = new()
            {
                X = 26,
                Y = row,
                Width = Dim.Fill(),
                Value = field.InitialValue,
            };
            inputs[field.Key] = input;
            window.Add(label, input);
            row += 2;
        }

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
        window.Add(save, cancel);

        app.Run(window);

        IReadOnlyDictionary<string, string> values = inputs.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Value ?? string.Empty,
            StringComparer.Ordinal);

        return new FieldEditResult(saved, values);
    }
}

internal sealed class TerminalGuiFieldEditorWindow : Window
{
    private readonly IApplication app;
    private readonly Action save;

    public TerminalGuiFieldEditorWindow(IApplication app, string title, Action save)
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
