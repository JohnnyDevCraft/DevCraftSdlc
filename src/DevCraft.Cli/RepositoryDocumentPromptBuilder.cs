namespace DevCraft.Cli;

public static class RepositoryDocumentPromptBuilder
{
    public static string BuildConstitutionImport(
        string repositoryDirectory,
        string constitutionPath,
        IReadOnlyList<string> allowedFiles,
        string? changeNote = null)
    {
        return Build(
            repositoryDirectory,
            allowedFiles,
            $"""
            Import the SpecKit constitution into this repository's DevCraft control files.

            SpecKit constitution: {constitutionPath}

            Read the constitution and every allowed DevCraft file that exists. Carry the constitution's principles, constraints, technology decisions, quality gates, and governance rules into the DevCraft file where each belongs: governance, compliance, and quality gates in GOV.md; architecture and technology constraints in ARCH.md; working agreements in AGENT.md; data rules in MODEL.md; product purpose in README.md or DISCOVERY.md; visual rules in THEME.md.
            This import may run again after the constitution changes. Update content that came from an earlier version of the constitution instead of duplicating it.
            {changeNote}
            """.TrimEnd());
    }

    public static string BuildInferProjectDetails(
        string repositoryDirectory,
        string? constitutionPath,
        string? specsDirectory,
        IReadOnlyList<string> allowedFiles)
    {
        List<string> sources = ["- The repository source code, project files, build and package manifests, README files, and configuration."];

        if (constitutionPath is not null)
        {
            sources.Add($"- SpecKit constitution: {constitutionPath}");
        }

        if (specsDirectory is not null)
        {
            sources.Add($"- SpecKit feature specs: {specsDirectory}");
        }

        return Build(
            repositoryDirectory,
            allowedFiles,
            $"""
            Infer this project's details from the existing repository and record them in the DevCraft control files.

            Sources to read:
            {string.Join(Environment.NewLine, sources)}

            ARCH.md: languages, frameworks, project structure, architecture style, and key design decisions.
            DISCOVERY.md: purpose, problems solved, actors, requirements, use cases, and existing capabilities.
            MODEL.md: entities, relationships, persistence, and constraints found in the code.
            GOV.md: security, compliance, operational, testing, and release rules found in the repository.
            Return THEME.md only when the application has a visual theme (colors, typography, styles, design tokens, or brand assets). Otherwise leave it out.
            """);
    }

    private static string Build(string repositoryDirectory, IReadOnlyList<string> allowedFiles, string task)
    {
        string devcraftDirectory = Path.Combine(repositoryDirectory, ".devcraft");

        return $$"""
            {{task}}

            Repository: {{repositoryDirectory}}
            DevCraft control folder: {{devcraftDirectory}}
            Allowed DevCraft files: {{string.Join(", ", allowedFiles)}}

            Rules:
            - Do not write files. DevCraft writes the files from your response.
            - Use the existing file as the base. Keep its structure, operator decisions, and anything you have no evidence to change.
            - Preserve the Project Status and Discovery Stage values exactly as they are.
            - Record only facts you found evidence for. Do not invent requirements, entities, or decisions.
            - Return a file only when its content should change, and return its complete new content.

            Respond with only this JSON object and nothing else:
            {
              "success": true,
              "summary": "One or two sentences describing what changed.",
              "files": [
                { "name": "GOV.md", "content": "complete Markdown content" }
              ]
            }
            Use "success": false with a summary explaining why when you cannot complete the work.
            """;
    }
}
