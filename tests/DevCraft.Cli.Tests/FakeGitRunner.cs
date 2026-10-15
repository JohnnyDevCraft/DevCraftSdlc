using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

// Scripted git runner. Unscripted commands succeed with empty output; the last scripted result for a command repeats.
public sealed class FakeGitRunner : IGitRunner
{
    private readonly Dictionary<string, Queue<TerminalProcessResult>> responses = [];

    public List<string> Calls { get; } = [];

    public FakeGitRunner On(string arguments, int exitCode = 0, string output = "", string error = "")
    {
        if (!responses.TryGetValue(arguments, out Queue<TerminalProcessResult>? queue))
        {
            queue = new Queue<TerminalProcessResult>();
            responses[arguments] = queue;
        }

        queue.Enqueue(new TerminalProcessResult(exitCode, output, error));
        return this;
    }

    public TerminalProcessResult Run(string workingDirectory, IReadOnlyList<string> arguments)
    {
        string key = string.Join(' ', arguments);
        Calls.Add(key);

        if (!responses.TryGetValue(key, out Queue<TerminalProcessResult>? queue) || queue.Count == 0)
        {
            return new TerminalProcessResult(0, string.Empty, string.Empty);
        }

        return queue.Count > 1 ? queue.Dequeue() : queue.Peek();
    }
}
