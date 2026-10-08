namespace DevCraft.Cli;

public sealed record TerminalProcessResult(int ExitCode, string StandardOutput, string StandardError);
