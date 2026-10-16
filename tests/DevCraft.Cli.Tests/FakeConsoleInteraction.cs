using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class FakeConsoleInteraction : IConsoleInteraction
{
    private readonly Queue<string> answers;
    private readonly Queue<string> selections;
    private readonly Queue<TextEditResult> textEdits = [];
    private readonly Queue<FieldEditResult> fieldEdits = [];
    private readonly Queue<ValueEditResult> valueEdits = [];
    private readonly Queue<IReadOnlyList<string>> multiSelections = [];

    public FakeConsoleInteraction(IEnumerable<string> answers, IEnumerable<string>? selections = null)
    {
        this.answers = new Queue<string>(answers);
        this.selections = new Queue<string>(selections ?? []);
    }

    public List<string> StatusMessages { get; } = [];

    public List<string> StartupStages { get; } = [];

    public List<(string Prompt, int StatusCountWhenAsked)> WaitPrompts { get; } = [];

    public List<string> SelectTitles { get; } = [];

    public List<IReadOnlyList<string>> SelectChoices { get; } = [];

    public List<string> AskPrompts { get; } = [];

    public List<string> SelectManyTitles { get; } = [];

    public List<IReadOnlyList<string>> SelectManyChoices { get; } = [];

    public List<string> ValueEditTitles { get; } = [];

    public List<string> ValueEditInitialValues { get; } = [];

    public List<string> TextEditTitles { get; } = [];

    public List<string> TextEditInitialTexts { get; } = [];

    public List<string> FieldEditTitles { get; } = [];

    public List<IReadOnlyList<FieldEditField>> FieldEditFields { get; } = [];

    public List<string> Events { get; } = [];

    public int MenuShellCount { get; private set; }

    public void EnqueueFieldEdit(IReadOnlyDictionary<string, string> values, bool saved = true)
    {
        fieldEdits.Enqueue(new FieldEditResult(saved, values));
    }

    public void EnqueueSelectMany(params string[] selected)
    {
        multiSelections.Enqueue(selected);
    }

    public void EnqueueValueEdit(string value, bool saved = true)
    {
        valueEdits.Enqueue(new ValueEditResult(saved, value));
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

    public void WaitForEnter(string prompt)
    {
        WaitPrompts.Add((prompt, StatusMessages.Count));
    }

    public string Ask(string prompt)
    {
        AskPrompts.Add(prompt);

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

    public ValueEditResult EditValue(string title, string label, string initialValue, bool required = false)
    {
        ValueEditTitles.Add(title);
        ValueEditInitialValues.Add(initialValue);

        return valueEdits.Count > 0
            ? valueEdits.Dequeue()
            : new ValueEditResult(true, initialValue);
    }

    public TextEditResult EditText(TextEditOptions options)
    {
        TextEditTitles.Add(options.Title);
        Events.Add($"text:{options.Title}");
        TextEditInitialTexts.Add(options.InitialText);

        return textEdits.Count > 0
            ? textEdits.Dequeue()
            : new TextEditResult(true, options.InitialText);
    }

    public string Select(string title, IReadOnlyList<string> choices)
    {
        SelectTitles.Add(title);
        Events.Add($"select:{title}");
        SelectChoices.Add(choices);

        if (selections.Count > 0)
        {
            return selections.Dequeue();
        }

        return choices[0];
    }

    public IReadOnlyList<string> SelectMany(string title, IReadOnlyList<string> choices)
    {
        SelectManyTitles.Add(title);
        Events.Add($"selectMany:{title}");
        SelectManyChoices.Add(choices);

        return multiSelections.Count > 0 ? multiSelections.Dequeue() : [];
    }

    public T RunStatus<T>(string message, Func<T> action)
    {
        StatusMessages.Add(message);

        return action();
    }
}
