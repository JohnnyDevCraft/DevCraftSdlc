using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class TerminalAiProjectScannerTests
{
    [Fact]
    public void ScanParsesControlledResponse()
    {
        const string response = """
        {
          "projectProfile": {
            "name": "Sample",
            "purpose": "Demonstrate scan parsing.",
            "description": "Sample demonstrates a simple console app."
          },
          "projects": [
            {
              "name": "Sample",
              "type": ".NET console app"
            }
          ],
          "aiDrivenSdlc": {
            "detected": false,
            "name": null
          }
        }
        """;
        string? original = Environment.GetEnvironmentVariable("DEVCRAFT_AI_SCAN_RESPONSE");
        Environment.SetEnvironmentVariable("DEVCRAFT_AI_SCAN_RESPONSE", response);

        try
        {
            TerminalAiProjectScanner scanner = new();

            ProjectScanResult result = scanner.Scan("/tmp/sample", "Codex");

            Assert.Equal("Sample", result.ProjectProfile.Name);
            DetectedProject project = Assert.Single(result.Projects);
            Assert.Equal("Sample", project.Name);
            Assert.Equal(".NET console app", project.Type);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DEVCRAFT_AI_SCAN_RESPONSE", original);
        }
    }
}
