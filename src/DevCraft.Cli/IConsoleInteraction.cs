namespace DevCraft.Cli;

public interface IConsoleInteraction
{
    void WriteStatus(string message);

    void ShowStartupStage(string message);

    void ShowMenuShell();

    string Ask(string prompt);

    string Select(string title, IReadOnlyList<string> choices);

    T RunStatus<T>(string message, Func<T> action);
}
