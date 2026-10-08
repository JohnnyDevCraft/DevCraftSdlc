using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class FakeAiProjectScanner : IAiProjectScanner
{
    private readonly ProjectScanResult result;

    public FakeAiProjectScanner(ProjectScanResult result)
    {
        this.result = result;
    }

    public bool WasCalled { get; private set; }

    public string? DefaultAgent { get; private set; }

    public string? ProfileDirectory { get; private set; }

    public ProjectScanResult Scan(string directoryPath, string defaultAgent, string profileDirectory)
    {
        WasCalled = true;
        DefaultAgent = defaultAgent;
        ProfileDirectory = profileDirectory;

        return result;
    }
}
