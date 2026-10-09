using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class FakeConsoleInteraction : IConsoleInteraction
{
    private readonly Queue<string> answers;
    private readonly Queue<string> selections;
    private readonly Queue<TextEditResult> textEdits = [];
    private readonly Queue<FieldEditResult> fieldEdits = [];

    public FakeConsoleInteraction(IEnumerable<string> answers, IEnumerable<string>? selections = null)
    {
        this.answers = new Queue<string>(answers);
        this.selections = new Queue<string>(selections ?? []);
    }

    public List<string> StatusMessages { get; } = [];

    public List<string> StartupStages { get; } = [];

    public List<string> SelectTitles { get; } = [];

    public List<IReadOnlyList<string>> SelectChoices { get; } = [];

    public List<string> TextEditTitles { get; } = [];

    public List<string> TextEditInitialTexts { get; } = [];

    public List<string> FieldEditTitles { get; } = [];

    public List<IReadOnlyList<FieldEditField>> FieldEditFields { get; } = [];

    public int MenuShellCount { get; private set; }

    public void EnqueueFieldEdit(IReadOnlyDictionary<string, string> values, bool saved = true)
    {
        fieldEdits.Enqueue(new FieldEditResult(saved, values));
    }

    public void EnqueueTextEdit(string text, bool saved = true)
    {
        textEdits.Enqueue(new TextEditResult(saved, text));
    }

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

    public FieldEditResult EditFields(FieldEditOptions options)
    {
        FieldEditTitles.Add(options.Title);
        FieldEditFields.Add(options.Fields);

        if (fieldEdits.Count > 0)
        {
            return fieldEdits.Dequeue();
        }

        Dictionary<string, string> values = [];

        foreach (FieldEditField field in options.Fields)
        {
            values[field.Key] = answers.Count > 0 ? answers.Dequeue() : field.InitialValue;
        }

        return new FieldEditResult(true, values);
    }

    public TextEditResult EditText(TextEditOptions options)
    {
        TextEditTitles.Add(options.Title);
        TextEditInitialTexts.Add(options.InitialText);

        return textEdits.Count > 0
            ? textEdits.Dequeue()
            : new TextEditResult(true, options.InitialText);
    }

    public string Select(string title, IReadOnlyList<string> choices)
    {
        SelectTitles.Add(title);
        SelectChoices.Add(choices);

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
