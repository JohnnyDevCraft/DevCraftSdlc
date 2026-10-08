namespace DevCraft.Cli;

public static class ProjectScanPromptBuilder
{
    public static string Build(string directoryPath)
    {
        return $$"""
Inspect this folder for DevCraft startup: {{directoryPath}}

Return only JSON with this shape:
{
  "projectProfile": {
    "name": "Overall project name",
    "purpose": "One-sentence explanation of what the project is for",
    "description": "Short practical summary of what the project does"
  },
  "projects": [
    {
      "name": "Project name",
      "type": "Project type"
    }
  ],
  "aiDrivenSdlc": {
    "detected": true,
    "name": "SpecKit, Superpowers, DevCraft, or another detected workflow"
  }
}

Identify the overall project name, what the project is for, what it does, project names, and project types. Also determine whether an AI-driven SDLC workflow is already in place, especially Superpowers or SpecKit.
""";
    }
}
