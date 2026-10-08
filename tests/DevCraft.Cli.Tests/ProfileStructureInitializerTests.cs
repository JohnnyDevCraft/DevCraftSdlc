using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class ProfileStructureInitializerTests
{
    [Fact]
    public void EnsureCreatesRequiredProfileFolders()
    {
        using TestDirectory root = new();
        using TestDirectory sourceRoot = new();
        string profile = Path.Combine(root.Path, ".DevCraft");
        Directory.CreateDirectory(profile);

        ProfileStructureResult result = ProfileStructureInitializer.Ensure(profile, sourceRoot.Path);

        Assert.True(Directory.Exists(Path.Combine(profile, "skills")));
        Assert.True(Directory.Exists(Path.Combine(profile, "standards")));
        Assert.True(Directory.Exists(Path.Combine(profile, "architectures")));
        Assert.True(Directory.Exists(Path.Combine(profile, "templates")));
        Assert.True(Directory.Exists(Path.Combine(profile, "project-types")));
        Assert.True(Directory.Exists(Path.Combine(profile, "feature-storage")));
        Assert.True(Directory.Exists(Path.Combine(profile, "features")));
        Assert.True(File.Exists(Path.Combine(profile, "features", "projects.json")));
        Assert.Equal(7, result.CreatedDirectories.Count);
    }

    [Fact]
    public void EnsureDoesNotReportExistingProfileFoldersAsCreated()
    {
        using TestDirectory root = new();
        using TestDirectory sourceRoot = new();
        string profile = Path.Combine(root.Path, ".DevCraft");
        Directory.CreateDirectory(Path.Combine(profile, "skills"));
        Directory.CreateDirectory(Path.Combine(profile, "standards"));
        Directory.CreateDirectory(Path.Combine(profile, "architectures"));
        Directory.CreateDirectory(Path.Combine(profile, "templates"));
        Directory.CreateDirectory(Path.Combine(profile, "project-types"));
        Directory.CreateDirectory(Path.Combine(profile, "feature-storage"));
        Directory.CreateDirectory(Path.Combine(profile, "features"));
        File.WriteAllText(Path.Combine(profile, "features", "projects.json"), "{ \"Projects\": [] }");

        ProfileStructureResult result = ProfileStructureInitializer.Ensure(profile, sourceRoot.Path);

        Assert.Empty(result.CreatedDirectories);
    }

    [Fact]
    public void EnsureCopiesBaseFilesAndWritesConfigureJson()
    {
        using TestDirectory root = new();
        using TestDirectory sourceRoot = new();
        string profile = Path.Combine(root.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        Directory.CreateDirectory(Path.Combine(sourceRoot.Path, "skills"));
        Directory.CreateDirectory(Path.Combine(sourceRoot.Path, "standards"));
        Directory.CreateDirectory(Path.Combine(sourceRoot.Path, "Architecture"));
        Directory.CreateDirectory(Path.Combine(sourceRoot.Path, "modes"));
        File.WriteAllText(
            Path.Combine(sourceRoot.Path, "skills", "Operator-Questions.md"),
            """
            # Skill: Operator Questions

            ## Intent

            Present material questions to the operator.
            """);
        File.WriteAllText(
            Path.Combine(sourceRoot.Path, "skills", "README.md"),
            """
            # Skills

            This folder documents skills.
            """);
        File.WriteAllText(
            Path.Combine(sourceRoot.Path, "skills", "_template.md"),
            """
            # Skill: Template

            This is not an installable skill.
            """);
        File.WriteAllText(
            Path.Combine(sourceRoot.Path, "standards", "CSharp.md"),
            """
            # C# Standards

            ## Purpose

            Define default coding standards for C# code.
            """);
        File.WriteAllText(
            Path.Combine(sourceRoot.Path, "Architecture", "SwiftUI-App.md"),
            """
            # SwiftUI App

            ## Purpose

            Define the default architecture guidance for SwiftUI applications.
            """);
        File.WriteAllText(
            Path.Combine(sourceRoot.Path, "modes", "DevCraft.md"),
            """
            # DevCraft Mode

            ## Purpose

            Define DevCraft workflow rules.
            """);
        Directory.CreateDirectory(Path.Combine(profile, "feature-storage"));
        File.WriteAllText(
            Path.Combine(profile, "feature-storage", "ado-work-item.md"),
            """
            # ADO Work Item Feature Storage

            ## Purpose

            Store DevCraft feature artifacts as Azure DevOps work item attachments.
            """);

        ProfileStructureInitializer.Ensure(profile, sourceRoot.Path);

        string configurePath = Path.Combine(profile, "configure.json");
        string configureJson = File.ReadAllText(configurePath);
        Assert.True(File.Exists(Path.Combine(profile, "skills", "Create-Project-Type.md")));
        Assert.True(File.Exists(Path.Combine(profile, "skills", "Create-Architecture.md")));
        Assert.True(File.Exists(Path.Combine(profile, "skills", "Create-Skill.md")));
        Assert.True(File.Exists(Path.Combine(profile, "skills", "Create-Standard.md")));
        Assert.True(File.Exists(Path.Combine(profile, "skills", "DevCraft-Import.md")));
        Assert.True(File.Exists(Path.Combine(profile, "skills", "Operator-Questions.md")));
        Assert.True(File.Exists(Path.Combine(profile, "templates", "skill-template.md")));
        Assert.True(File.Exists(Path.Combine(profile, "templates", "project-type-template.md")));
        Assert.True(File.Exists(Path.Combine(profile, "templates", "architecture-template.md")));
        Assert.True(File.Exists(Path.Combine(profile, "templates", "standard-template.md")));
        Assert.True(File.Exists(Path.Combine(profile, "DevCraft.md")));
        Assert.True(File.Exists(Path.Combine(profile, "initialized.md")));
        Assert.True(File.Exists(Path.Combine(profile, "features", "projects.json")));
        Assert.True(File.Exists(Path.Combine(profile, "standards", "CSharp.md")));
        Assert.True(File.Exists(Path.Combine(profile, "architectures", "SwiftUI-App.md")));
        Assert.Contains("\"Skills\"", configureJson);
        Assert.Contains("\"Slug\"", configureJson);
        Assert.Contains("\"operator-questions\"", configureJson);
        Assert.Contains("\"Operator Questions\"", configureJson);
        Assert.Contains("Present material questions to the operator.", configureJson);
        Assert.Contains("\"create-project-type\"", configureJson);
        Assert.Contains("\"Create Project Type\"", configureJson);
        Assert.Contains("\"create-architecture\"", configureJson);
        Assert.Contains("\"Create Architecture\"", configureJson);
        Assert.Contains("\"create-skill\"", configureJson);
        Assert.Contains("\"Create Skill\"", configureJson);
        Assert.Contains("\"create-standard\"", configureJson);
        Assert.Contains("\"Create Standard\"", configureJson);
        Assert.Contains("\"devcraft-import\"", configureJson);
        Assert.Contains("\"DevCraft Import\"", configureJson);
        Assert.DoesNotContain("\"readme\"", configureJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("_template", configureJson);
        Assert.Contains("\"Standards\"", configureJson);
        Assert.Contains("\"C# Standards\"", configureJson);
        Assert.Contains("\"Architectures\"", configureJson);
        Assert.Contains("\"swiftui-app\"", configureJson);
        Assert.Contains("\"SwiftUI App\"", configureJson);
        Assert.Contains("\"Templates\"", configureJson);
        Assert.Contains("\"skill-template\"", configureJson);
        Assert.Contains("\"project-type-template\"", configureJson);
        Assert.Contains("\"architecture-template\"", configureJson);
        Assert.Contains("\"standard-template\"", configureJson);
        Assert.Contains("\"ProjectTypes\"", configureJson);
        Assert.Contains("\"FeatureStorageTypes\"", configureJson);
        Assert.DoesNotContain("\"SelectedFeatureStorage\"", configureJson);
        Assert.Contains("\"ado-work-item\"", configureJson);
        Assert.Contains("\"SupportedClients\"", configureJson);
        Assert.Contains("\"Scan\"", configureJson);
        Assert.Contains("\"Session\"", configureJson);
        Assert.Contains("\"Arguments\"", configureJson);
        Assert.Contains("\"codex\"", configureJson);
        Assert.Contains("\"claude-code\"", configureJson);
        Assert.Contains("\"github-copilot\"", configureJson);
    }
}
