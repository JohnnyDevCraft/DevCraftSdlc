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

    [Fact]
    public void ParseReadsJsonWrappedInMarkdownCodeFence()
    {
        const string json = """
        ```json
        {
          "projectProfile": {
            "name": "Wrapped Project",
            "purpose": "Verify wrapped JSON.",
            "description": "The AI returned Markdown."
          },
          "projects": [],
          "aiDrivenSdlc": {
            "detected": false,
            "name": null
          }
        }
        ```
        """;

        ProjectScanResult result = ProjectScanJsonParser.Parse(json);

        Assert.Equal("Wrapped Project", result.ProjectProfile.Name);
        Assert.Equal("Verify wrapped JSON.", result.ProjectProfile.Purpose);
        Assert.Equal("The AI returned Markdown.", result.ProjectProfile.Description);
    }

    [Fact]
    public void ParseReadsJsonWithIntroductoryText()
    {
        const string json = """
        Here is the scan result:

        {
          "projectProfile": {
            "name": "Text Wrapped Project",
            "purpose": "Verify text before JSON.",
            "description": "The AI returned a sentence before the JSON."
          },
          "projects": [],
          "aiDrivenSdlc": {
            "detected": false,
            "name": null
          }
        }
        """;

        ProjectScanResult result = ProjectScanJsonParser.Parse(json);

        Assert.Equal("Text Wrapped Project", result.ProjectProfile.Name);
    }

    [Fact]
    public void ParseThrowsInvalidOperationWhenJsonIsInvalid()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => ProjectScanJsonParser.Parse("```not json```"));

        Assert.Contains("valid project scan JSON", exception.Message);
    }
}
