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

    public ProjectScanResult Scan(string directoryPath, string defaultAgent)
    {
        WasCalled = true;

        return result;
    }
}

