using Terminal.Gui.App;
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

        Label validation = new()
        {
            Text = string.Empty,
            X = 0,
            Y = Pos.AnchorEnd(2),
            Width = Dim.Fill(),
        };

        using TerminalGuiFieldEditorWindow window = new(
            app,
            $"{options.Title} - Ctrl+S Save, Esc Cancel",
            () =>
            {
                FieldEditField? missing = FieldEditValidation.FirstMissingRequired(options.Fields, ReadValues(inputs));

                if (missing is not null)
                {
                    validation.Text = $"{missing.Label} is required.";
                    inputs[missing.Key].SetFocus();
                    return false;
                }

                saved = true;
                return true;
            });

        int row = TerminalGuiHeader.AddTo(window, app.Screen.Height);

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
        window.Add(validation, save, cancel);

        app.Run(window);

        return new FieldEditResult(saved, ReadValues(inputs));
    }

    private static IReadOnlyDictionary<string, string> ReadValues(Dictionary<string, TextField> inputs)
    {
        return inputs.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Value ?? string.Empty,
            StringComparer.Ordinal);
    }
}
