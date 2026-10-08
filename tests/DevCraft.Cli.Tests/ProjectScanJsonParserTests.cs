using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class ProjectScanJsonParserTests
{
    [Fact]
    public void ParseReadsProjectsAndAiDrivenSdlc()
    {
        const string json = """
        {
          "projectProfile": {
            "name": "DevCraft",
            "purpose": "Manage AI-assisted SDLC workflows.",
            "description": "DevCraft is a console tool for project setup and workflow control."
          },
          "projects": [
            {
              "name": "DevCraft.Cli",
              "type": ".NET console app"
            }
          ],
          "aiDrivenSdlc": {
            "detected": true,
            "name": "SpecKit"
          }
        }
        """;

        ProjectScanResult result = ProjectScanJsonParser.Parse(json);

        Assert.Equal("DevCraft", result.ProjectProfile.Name);
        Assert.Equal("Manage AI-assisted SDLC workflows.", result.ProjectProfile.Purpose);
        Assert.Equal("DevCraft is a console tool for project setup and workflow control.", result.ProjectProfile.Description);
        DetectedProject project = Assert.Single(result.Projects);
        Assert.Equal("DevCraft.Cli", project.Name);
        Assert.Equal(".NET console app", project.Type);
        Assert.True(result.AiDrivenSdlc.Detected);
        Assert.Equal("SpecKit", result.AiDrivenSdlc.Name);
    }

    [Fact]
    public void ParseUsesFallbackProjectProfile()
    {
        const string json = """
        {
          "projects": [],
          "aiDrivenSdlc": {
            "detected": false,
            "name": null
          }
        }
        """;

        ProjectScanResult result = ProjectScanJsonParser.Parse(json);

        Assert.Equal("Unknown Project", result.ProjectProfile.Name);
        Assert.Equal("Pending.", result.ProjectProfile.Purpose);
        Assert.Equal("Pending.", result.ProjectProfile.Description);
    }
}
