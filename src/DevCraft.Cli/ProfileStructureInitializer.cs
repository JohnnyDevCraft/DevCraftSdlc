namespace DevCraft.Cli;

using System.Text.Json;

public static class ProfileStructureInitializer
{
    private static readonly string[] RequiredDirectories =
    [
        "skills",
        "standards",
        "architectures",
        "templates",
        "project-types",
        "feature-storage",
        "features",
    ];

    public static ProfileStructureResult Ensure(string profileDirectory)
    {
        string sharedGuidanceRoot = Environment.GetEnvironmentVariable("DEVCRAFT_SHARED_GUIDANCE_HOME")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "codex-setup");

        return Ensure(profileDirectory, sharedGuidanceRoot);
    }

    public static ProfileStructureResult Ensure(string profileDirectory, string sharedGuidanceRoot)
    {
        List<string> createdDirectories = [];
        List<string> createdFiles = [];
        List<string> updatedFiles = [];

        foreach (string directory in RequiredDirectories)
        {
            string path = Path.Combine(profileDirectory, directory);

            if (Directory.Exists(path))
            {
                continue;
            }

            Directory.CreateDirectory(path);
            createdDirectories.Add(path);
        }

        CopyDocuments(sharedGuidanceRoot, profileDirectory, "skills", "skills", createdFiles);
        EnsureCreateProjectTypeSkill(profileDirectory, createdFiles);
        EnsureCreateArchitectureSkill(profileDirectory, createdFiles);
        EnsureCreateSkillSkill(profileDirectory, createdFiles);
        EnsureCreateStandardSkill(profileDirectory, createdFiles);
        EnsureDevCraftImportSkill(profileDirectory, createdFiles);
        CopyDocuments(sharedGuidanceRoot, profileDirectory, "standards", "standards", createdFiles);
        CopyDocuments(sharedGuidanceRoot, profileDirectory, "Architecture", "architectures", createdFiles);
        CopySkillTemplate(sharedGuidanceRoot, profileDirectory, createdFiles);
        CopyDevCraftTemplates(sharedGuidanceRoot, profileDirectory, createdFiles);
        EnsureProjectTypeTemplate(profileDirectory, createdFiles);
        EnsureArchitectureTemplate(profileDirectory, createdFiles);
        EnsureStandardTemplate(profileDirectory, createdFiles);
        CopyDevCraftRules(sharedGuidanceRoot, profileDirectory, createdFiles);
        EnsureInitializedFile(profileDirectory, createdFiles);
        EnsureSystemCentralProjectIndex(profileDirectory, createdDirectories, createdFiles);
        WriteConfiguration(profileDirectory, createdFiles, updatedFiles);

        return new ProfileStructureResult(createdDirectories, createdFiles, updatedFiles);
    }

    private static void EnsureCreateProjectTypeSkill(string profileDirectory, List<string> createdFiles)
    {
        string targetPath = Path.Combine(profileDirectory, "skills");
        Directory.CreateDirectory(targetPath);
        string targetFile = Path.Combine(targetPath, "Create-Project-Type.md");

        if (File.Exists(targetFile))
        {
            return;
        }

        File.WriteAllText(
            targetFile,
            """
            # Skill: Create Project Type

            ## Intent

            Help the operator design a reusable DevCraft project type through a focused back-and-forth conversation, then produce a completed project type document from the project type template.

            ## Triggers

            - The operator asks to create, design, define, or refine a DevCraft project type.
            - DevCraft needs a reusable project type before it can scaffold or guide a class of projects.
            - A new project pattern should become repeatable across repositories.

            ## Inputs

            - The project type name or working name.
            - The kind of software or repository the project type creates.
            - Any required standards, architectures, skills, frameworks, commands, or project layout rules.
            - The profile template at `templates/project-type-template.md`.

            ## Conversation Workflow

            1. Start by asking what kind of project the operator wants DevCraft to create or recognize.
            2. Ask what problem that project type solves and what a successful generated project should contain.
            3. Ask which standards, architectures, and skills should be associated with the project type.
            4. Ask what setup steps DevCraft should follow, including project names, project kinds, physical paths, and optional solution folder paths.
            5. Repeat the proposed project type back to the operator as a short structured summary before writing it.
            6. Ask the operator to approve, revise, or add missing details.
            7. After approval, create the project type document from `templates/project-type-template.md` and write it to the `project-types` folder.

            ## Communication Rules

            - Ask one focused group of questions at a time.
            - Use clear options when the operator is choosing between likely project patterns.
            - Prefer the operator's words for project purpose, naming, and setup language.
            - Do not invent mandatory standards, architectures, or skills when the operator has not chosen them.
            - Confirm the proposed project type before creating or updating files.
            - Keep the conversation practical: every answer should help fill in the project type template.

            ## Output / Done Definition

            - A project type Markdown file exists under `project-types`.
            - The file was created from `templates/project-type-template.md`.
            - The file has a clear project type name, description, default standards, default architectures, and setup guidance.
            - The setup guidance is specific enough for DevCraft to scaffold or guide that project type later.
            - `configure.json` is updated so the new project type appears with a stable slug, description, and path.

            """);
        createdFiles.Add(targetFile);
    }

    private static void EnsureCreateArchitectureSkill(string profileDirectory, List<string> createdFiles)
    {
        string targetPath = Path.Combine(profileDirectory, "skills");
        Directory.CreateDirectory(targetPath);
        string targetFile = Path.Combine(targetPath, "Create-Architecture.md");

        if (File.Exists(targetFile))
        {
            return;
        }

        File.WriteAllText(
            targetFile,
            """
            # Skill: Create Architecture

            ## Intent

            Help the operator design a reusable DevCraft architecture through a focused back-and-forth conversation, then produce a completed architecture document from the architecture template.

            ## Triggers

            - The operator asks to create, design, define, or refine a DevCraft architecture.
            - DevCraft needs reusable architecture guidance before planning or generating a project.
            - A recurring structural pattern should become available across project types and repositories.

            ## Inputs

            - The architecture name or working name.
            - The kind of system, application, service, or repository the architecture supports.
            - The constraints, tradeoffs, boundaries, integrations, and testing expectations the architecture should describe.
            - The profile template at `templates/architecture-template.md`.

            ## Conversation Workflow

            1. Ask what kind of system or project the architecture should guide.
            2. Ask what problem the architecture solves and when DevCraft should recommend it.
            3. Ask for the core principles, major components, boundaries, data flow, and integration expectations.
            4. Ask what the architecture should avoid or warn against.
            5. Ask what testing, deployment, and operational concerns belong in the architecture.
            6. Repeat the proposed architecture back to the operator as a short structured summary before writing it.
            7. Ask the operator to approve, revise, or add missing details.
            8. After approval, create the architecture document from `templates/architecture-template.md` and write it to the `architectures` folder.

            ## Communication Rules

            - Ask one focused group of questions at a time.
            - Use clear options when the operator is choosing between common architecture patterns.
            - Prefer the operator's words for the architecture purpose, names, boundaries, and tradeoffs.
            - Do not invent mandatory technologies, hosting platforms, or patterns when the operator has not chosen them.
            - Confirm the proposed architecture before creating or updating files.
            - Keep the conversation practical: every answer should help fill in the architecture template.

            ## Output / Done Definition

            - An architecture Markdown file exists under `architectures`.
            - The file was created from `templates/architecture-template.md`.
            - The file has a clear architecture name, purpose, usage guidance, principles, structure, boundaries, data and integration notes, testing guidance, and avoid list.
            - `configure.json` is updated so the new architecture appears with a stable slug, description, and path.

            """);
        createdFiles.Add(targetFile);
    }

    private static void CopyDevCraftTemplates(string sharedGuidanceRoot, string profileDirectory, List<string> createdFiles)
    {
        string sourcePath = Path.Combine(sharedGuidanceRoot, "modes", "DevCraft", "templates");
        string targetPath = Path.Combine(profileDirectory, "templates");

        if (!Directory.Exists(sourcePath))
        {
            return;
        }

        Directory.CreateDirectory(targetPath);

        foreach (string sourceFile in Directory.EnumerateFiles(sourcePath, "*.md").OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            string targetFile = Path.Combine(targetPath, Path.GetFileName(sourceFile));

            if (File.Exists(targetFile))
            {
                continue;
            }

            File.Copy(sourceFile, targetFile);
            createdFiles.Add(targetFile);
        }
    }

    private static void EnsureCreateSkillSkill(string profileDirectory, List<string> createdFiles)
    {
        string targetPath = Path.Combine(profileDirectory, "skills");
        Directory.CreateDirectory(targetPath);
        string targetFile = Path.Combine(targetPath, "Create-Skill.md");

        if (File.Exists(targetFile))
        {
            return;
        }

        File.WriteAllText(
            targetFile,
            """
            # Skill: Create Skill

            ## Intent

            Help the operator design a reusable DevCraft skill through a focused back-and-forth conversation, then produce a completed skill document from the skill template.

            ## Triggers

            - The operator asks to create, design, define, or refine a DevCraft skill.
            - DevCraft needs repeatable instructions for a workflow, task type, artifact type, or decision process.
            - A recurring way of working should become available across projects.

            ## Inputs

            - The skill name or working name.
            - The situations or operator phrasing that should trigger the skill.
            - The information the AI needs from the operator, repository, or profile DevCraft folder.
            - The workflow the AI should follow.
            - The output or done definition for the skill.
            - The profile template at `templates/skill-template.md`.

            ## Conversation Workflow

            1. Ask what recurring DevCraft workflow or behavior the skill should capture.
            2. Ask when the skill should be used, including explicit trigger phrases and project situations.
            3. Ask what inputs the AI needs before it can use the skill safely.
            4. Ask for the workflow steps the AI should follow.
            5. Ask what output, files, decisions, or user-visible results prove the skill is complete.
            6. Repeat the proposed skill back to the operator as a short structured summary before writing it.
            7. Ask the operator to approve, revise, or add missing details.
            8. After approval, create the skill document from `templates/skill-template.md` and write it to the `skills` folder.

            ## Communication Rules

            - Ask one focused group of questions at a time.
            - Use clear options when the operator is choosing between likely skill scopes.
            - Prefer the operator's words for skill intent, trigger language, and done definitions.
            - Do not invent required inputs, authority, or file changes when the operator has not chosen them.
            - Confirm the proposed skill before creating or updating files.
            - Keep the conversation practical: every answer should help fill in the skill template.

            ## Output / Done Definition

            - A skill Markdown file exists under `skills`.
            - The file was created from `templates/skill-template.md`.
            - The file has a clear skill name, intent, triggers, inputs, workflow, and output or done definition.
            - `configure.json` is updated so the new skill appears with a stable slug, description, and path.

            """);
        createdFiles.Add(targetFile);
    }

    private static void EnsureCreateStandardSkill(string profileDirectory, List<string> createdFiles)
    {
        string targetPath = Path.Combine(profileDirectory, "skills");
        Directory.CreateDirectory(targetPath);
        string targetFile = Path.Combine(targetPath, "Create-Standard.md");

        if (File.Exists(targetFile))
        {
            return;
        }

        File.WriteAllText(
            targetFile,
            """
            # Skill: Create Standard

            ## Intent

            Help the operator design a reusable DevCraft standard through a focused back-and-forth conversation, then produce a completed standard document from the standard template.

            ## Triggers

            - The operator asks to create, design, define, or refine a DevCraft standard.
            - DevCraft needs repeatable coding, documentation, testing, design, governance, or operational guidance.
            - A team or project convention should become available across project types and repositories.

            ## Inputs

            - The standard name or working name.
            - The language, framework, artifact, or practice the standard governs.
            - The baseline authority or existing convention the standard should start from.
            - Required rules, preferences, examples, avoidances, and testing expectations.
            - The profile template at `templates/standard-template.md`.

            ## Conversation Workflow

            1. Ask what practice, language, framework, artifact, or workflow the standard should govern.
            2. Ask what baseline source or existing convention should anchor the standard.
            3. Ask which rules are mandatory and which are preferences.
            4. Ask what examples, testing expectations, and avoidances should be included.
            5. Repeat the proposed standard back to the operator as a short structured summary before writing it.
            6. Ask the operator to approve, revise, or add missing details.
            7. After approval, create the standard document from `templates/standard-template.md` and write it to the `standards` folder.

            ## Communication Rules

            - Ask one focused group of questions at a time.
            - Use clear options when the operator is choosing between common convention patterns.
            - Prefer the operator's words for rules, exceptions, and avoidances.
            - Do not invent mandatory rules, external authorities, or compliance requirements when the operator has not chosen them.
            - Confirm the proposed standard before creating or updating files.
            - Keep the conversation practical: every answer should help fill in the standard template.

            ## Output / Done Definition

            - A standard Markdown file exists under `standards`.
            - The file was created from `templates/standard-template.md`.
            - The file has a clear standard name, purpose, baseline, principles, guidance sections, testing expectations, avoid list, and notes.
            - `configure.json` is updated so the new standard appears with a stable slug, description, and path.

            """);
        createdFiles.Add(targetFile);
    }

    private static void EnsureDevCraftImportSkill(string profileDirectory, List<string> createdFiles)
    {
        string targetPath = Path.Combine(profileDirectory, "skills");
        Directory.CreateDirectory(targetPath);
        string targetFile = Path.Combine(targetPath, "DevCraft-Import.md");

        if (File.Exists(targetFile))
        {
            return;
        }

        File.WriteAllText(
            targetFile,
            """
            # Skill: DevCraft Import

            ## Intent

            Import reusable DevCraft documentation from a local documentation repository into the profile-level DevCraft folder, then update the profile catalog in `configure.json`.

            ## Triggers

            - The operator chooses Configure DevCraft > Import Settings.
            - The operator provides a local folder containing reusable DevCraft documentation.
            - DevCraft needs to merge standards, architectures, project types, or skills from another repository into the local profile.

            ## Inputs

            - The local import source folder.
            - The profile-level DevCraft folder.
            - The profile `configure.json` file.
            - Source folders named `standards`, `architectures`, `project-types`, and `skills`.

            ## Import Mapping

            - Source `standards` Markdown files go to profile `standards`.
            - Source `architectures` Markdown files go to profile `architectures`.
            - Source `project-types` Markdown files go to profile `project-types`.
            - Source `skills` Markdown files go to profile `skills`.

            ## Catalog Rules

            Each imported Markdown file must have an entry in the matching `configure.json` section:

            - Standards go into `Standards`.
            - Architectures go into `Architectures`.
            - Project types go into `ProjectTypes`.
            - Skills go into `Skills`.

            Each catalog entry must include:

            - `Slug`: a unique lowercase kebab-case identifier generated from the document name.
            - `Name`: the readable document name.
            - `Description`: the document purpose, intent, or first meaningful paragraph.
            - `Path`: the profile-relative path to the imported file.

            If a slug already exists, add a numeric suffix such as `-2`, `-3`, or the next available number.

            ## Workflow

            1. Verify the import source folder exists.
            2. Inspect only the supported source folders: `standards`, `architectures`, `project-types`, and `skills`.
            3. Ignore non-Markdown files, `README.md`, and underscore-prefixed template files unless the operator explicitly asks to import them.
            4. Copy each valid Markdown file into the matching profile-level DevCraft folder.
            5. Preserve existing local files unless the operator explicitly approves an overwrite.
            6. Generate or refresh the matching catalog entries in profile `configure.json`.
            7. Report what was imported, what was skipped, and whether any names or slugs needed collision handling.

            ## Output / Done Definition

            - Imported Markdown files exist in the correct profile-level DevCraft folders.
            - Profile `configure.json` includes every imported item in the correct catalog section.
            - Every imported item has a stable slug, readable name, description, and profile-relative path.
            - The operator receives a concise import summary with imported, skipped, and collision-handled files.

            """);
        createdFiles.Add(targetFile);
    }

    private static void EnsureSystemCentralProjectIndex(
        string profileDirectory,
        List<string> createdDirectories,
        List<string> createdFiles)
    {
        string featuresDirectory = Path.Combine(profileDirectory, "features");

        if (!Directory.Exists(featuresDirectory))
        {
            Directory.CreateDirectory(featuresDirectory);
            createdDirectories.Add(featuresDirectory);
        }

        string projectsPath = Path.Combine(featuresDirectory, "projects.json");

        if (File.Exists(projectsPath))
        {
            return;
        }

        File.WriteAllText(
            projectsPath,
            """
            {
              "Projects": []
            }
            """);
        createdFiles.Add(projectsPath);
    }

    private static void EnsureProjectTypeTemplate(string profileDirectory, List<string> createdFiles)
    {
        string targetPath = Path.Combine(profileDirectory, "templates");
        Directory.CreateDirectory(targetPath);
        string targetFile = Path.Combine(targetPath, "project-type-template.md");

        if (File.Exists(targetFile))
        {
            return;
        }

        File.WriteAllText(
            targetFile,
            """
            # Project Type: (Name)

            ## Purpose

            One sentence: what kind of project this describes and when DevCraft should use it.

            ## Detection Signals

            List the files, folders, package references, commands, frameworks, or conventions that identify this project type.

            ## Default Standards

            List the standards DevCraft should prefer when working in this project type.

            ## Default Architectures

            List the architectures DevCraft should consider first for this project type.

            ## Default Skills

            List the skills DevCraft should load or recommend for this project type.

            ## Setup Guidance

            Describe the files, commands, and decisions needed to initialize or maintain this project type.

            ## Done Definition

            Describe what must be true for DevCraft to treat this project type definition as complete and usable.

            """);
        createdFiles.Add(targetFile);
    }

    private static void EnsureArchitectureTemplate(string profileDirectory, List<string> createdFiles)
    {
        string targetPath = Path.Combine(profileDirectory, "templates");
        Directory.CreateDirectory(targetPath);
        string targetFile = Path.Combine(targetPath, "architecture-template.md");

        if (File.Exists(targetFile))
        {
            return;
        }

        File.WriteAllText(
            targetFile,
            """
            # Architecture: (Name)

            ## Purpose

            One sentence: what this architecture enables and when DevCraft should use it.

            ## When To Use

            - Situation or project type where this architecture is a good fit.
            - Constraints or goals that make this architecture appropriate.

            ## Core Principles

            - Principle 1
            - Principle 2
            - Principle 3

            ## Recommended Structure

            Describe the primary folders, projects, services, modules, or layers this architecture expects.

            ## Boundaries

            Describe ownership, responsibility boundaries, dependency rules, and communication paths.

            ## Data and Integration

            Describe persistence, external systems, messaging, APIs, and integration rules.

            ## Testing

            Describe the testing strategy that should be expected for this architecture.

            ## Avoid

            - Pattern, dependency, or shortcut this architecture should avoid.
            - Failure mode DevCraft should watch for.

            """);
        createdFiles.Add(targetFile);
    }

    private static void EnsureStandardTemplate(string profileDirectory, List<string> createdFiles)
    {
        string targetPath = Path.Combine(profileDirectory, "templates");
        Directory.CreateDirectory(targetPath);
        string targetFile = Path.Combine(targetPath, "standard-template.md");

        if (File.Exists(targetFile))
        {
            return;
        }

        File.WriteAllText(
            targetFile,
            """
            # Standard: (Name)

            ## Purpose

            One sentence: what this standard governs and when DevCraft should apply it.

            ## Baseline

            Describe the published baseline, existing team convention, framework guidance, or source material this standard starts from.

            ## Core Principles

            - Principle 1
            - Principle 2
            - Principle 3

            ## Guidance

            Describe the rules, preferences, naming patterns, structure, workflow, or decision criteria that belong to this standard.

            ## Examples

            Add short examples when they help clarify expected usage.

            ## Testing

            Describe any verification, testing, review, or validation expectations tied to this standard.

            ## Avoid

            - Pattern, shortcut, or behavior this standard should prevent.
            - Common failure mode DevCraft should watch for.

            ## Notes

            Add precedence rules, exceptions, or project-specific override guidance.

            """);
        createdFiles.Add(targetFile);
    }

    private static void CopyDevCraftRules(
        string sharedGuidanceRoot,
        string profileDirectory,
        List<string> createdFiles)
    {
        string sourceFile = Path.Combine(sharedGuidanceRoot, "modes", "DevCraft.md");

        if (!File.Exists(sourceFile))
        {
            return;
        }

        string targetFile = Path.Combine(profileDirectory, "DevCraft.md");

        if (File.Exists(targetFile))
        {
            return;
        }

        File.Copy(sourceFile, targetFile);
        createdFiles.Add(targetFile);
    }

    private static void EnsureInitializedFile(string profileDirectory, List<string> createdFiles)
    {
        string path = Path.Combine(profileDirectory, "initialized.md");

        if (File.Exists(path))
        {
            return;
        }

        InitializedFileWriter.Ensure(profileDirectory);
        createdFiles.Add(path);
    }

    private static void CopySkillTemplate(
        string sharedGuidanceRoot,
        string profileDirectory,
        List<string> createdFiles)
    {
        string sourceFile = Path.Combine(sharedGuidanceRoot, "skills", "_template.md");

        if (!File.Exists(sourceFile))
        {
            return;
        }

        string targetPath = Path.Combine(profileDirectory, "templates");
        Directory.CreateDirectory(targetPath);
        string targetFile = Path.Combine(targetPath, "skill-template.md");

        if (File.Exists(targetFile))
        {
            return;
        }

        File.Copy(sourceFile, targetFile);
        createdFiles.Add(targetFile);
    }

    private static void CopyDocuments(
        string sharedGuidanceRoot,
        string profileDirectory,
        string sourceFolder,
        string targetFolder,
        List<string> createdFiles)
    {
        string sourcePath = Path.Combine(sharedGuidanceRoot, sourceFolder);

        if (!Directory.Exists(sourcePath))
        {
            return;
        }

        string targetPath = Path.Combine(profileDirectory, targetFolder);
        Directory.CreateDirectory(targetPath);

        foreach (string sourceFile in Directory.EnumerateFiles(sourcePath, "*.md").OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            string targetFile = Path.Combine(targetPath, Path.GetFileName(sourceFile));

            if (File.Exists(targetFile))
            {
                continue;
            }

            File.Copy(sourceFile, targetFile);
            createdFiles.Add(targetFile);
        }
    }

    private static void WriteConfiguration(
        string profileDirectory,
        List<string> createdFiles,
        List<string> updatedFiles)
    {
        string configurePath = Path.Combine(profileDirectory, "configure.json");
        DevCraftProfileConfiguration configuration = new(
            ProfileDocumentCatalogBuilder.Build(profileDirectory, "skills"),
            ProfileDocumentCatalogBuilder.Build(profileDirectory, "standards"),
            ProfileDocumentCatalogBuilder.Build(profileDirectory, "architectures"),
            ProfileDocumentCatalogBuilder.Build(profileDirectory, "templates"),
            ProfileDocumentCatalogBuilder.Build(profileDirectory, "project-types"),
            ProfileDocumentCatalogBuilder.Build(profileDirectory, "feature-storage"),
            "repo-central",
            SupportedTerminalClientCatalog.Create());

        string json = JsonSerializer.Serialize(
            configuration,
            new JsonSerializerOptions
            {
                WriteIndented = true,
            });

        if (File.Exists(configurePath))
        {
            updatedFiles.Add(configurePath);
        }
        else
        {
            createdFiles.Add(configurePath);
        }

        File.WriteAllText(configurePath, json);
    }
}
