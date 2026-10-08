using Spectre.Console;

namespace DevCraft.Cli;

public sealed class SpectreConsoleInteraction : IConsoleInteraction
{
    public void WriteStatus(string message)
    {
        AnsiConsole.WriteLine(message);
    }

    public void ShowStartupStage(string message)
    {
        ShowMenuShell();
        AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("green"))
            .Start(Markup.Escape(message), _ => Thread.Sleep(150));
    }

    public void ShowMenuShell()
    {
        AnsiConsole.Clear();
        ConsoleLogoWriter.Write(CliLogoRenderer.Create());
        AnsiConsole.Write(new Rule().RuleStyle("grey"));
    }

    public string Ask(string prompt)
    {
        ShowMenuShell();

        return AnsiConsole.Ask<string>(Markup.Escape(prompt));
    }

    public string Select(string title, IReadOnlyList<string> choices)
    {
        ShowMenuShell();

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(Markup.Escape(title))
                .AddChoices(choices));
    }

    public T RunStatus<T>(string message, Func<T> action)
    {
        return AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("green"))
            .Start(Markup.Escape(message), _ => action());
    }
}
