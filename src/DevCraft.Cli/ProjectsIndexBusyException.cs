namespace DevCraft.Cli;

public sealed class ProjectsIndexBusyException()
    : InvalidOperationException("projects.json is busy in another DevCraft session. Try again.");
