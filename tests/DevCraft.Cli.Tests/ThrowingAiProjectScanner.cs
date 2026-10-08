using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class ThrowingAiProjectScanner : IAiProjectScanner
{
    public ProjectScanResult Scan(string directoryPath, string defaultAgent, string profileDirectory)
    {
        throw new InvalidOperationException("Simulated scanner failure.");
    }
}
