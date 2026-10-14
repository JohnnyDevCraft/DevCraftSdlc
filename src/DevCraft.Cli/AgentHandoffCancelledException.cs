namespace DevCraft.Cli;

public sealed class AgentHandoffCancelledException : Exception
{
    public const string Notice = "Agent handoff cancelled.";

    public AgentHandoffCancelledException()
        : base(Notice)
    {
    }
}
