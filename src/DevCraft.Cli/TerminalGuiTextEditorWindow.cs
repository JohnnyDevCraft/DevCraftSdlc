using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace DevCraft.Cli;

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
