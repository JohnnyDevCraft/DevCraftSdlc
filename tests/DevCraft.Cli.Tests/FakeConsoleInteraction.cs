using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class FakeConsoleInteraction : IConsoleInteraction
{
    private readonly Queue<string> answers;
    private readonly Queue<string> selections;

    public FakeConsoleInteraction(IEnumerable<string> answers, IEnumerable<string>? selections = null)
    {
        this.answers = new Queue<string>(answers);
        this.selections = new Queue<string>(selections ?? []);
    }

    public List<string> StatusMessages { get; } = [];

    public List<string> StartupStages { get; } = [];

    public List<string> SelectTitles { get; } = [];

    public int MenuShellCount { get; private set; }

    public void WriteStatus(string message)
    {
        StatusMessages.Add(message);
    }

    public void ShowStartupStage(string message)
    {
        StartupStages.Add(message);
    }

    public void ShowMenuShell()
    {
        MenuShellCount++;
    }

    public string Ask(string prompt)
    {
        return answers.Dequeue();
    }

    public string Select(string title, IReadOnlyList<string> choices)
    {
        SelectTitles.Add(title);

        if (selections.Count > 0)
        {
            return selections.Dequeue();
        }

        return choices[0];
    }

    public T RunStatus<T>(string message, Func<T> action)
    {
        StatusMessages.Add(message);

        return action();
    }
}
