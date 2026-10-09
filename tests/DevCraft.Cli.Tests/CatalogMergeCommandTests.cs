using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class CatalogMergeCommandTests
{
    [Fact]
    public void MergeOverwritesDuplicateSlugsAndAppendsNewEntries()
    {
        DevCraftProfileConfiguration target = new(
            [
                new ProfileCatalogDocument("create-api", "Create API", "Old skill.", "skills/Create-API.md"),
            ],
            [
                new ProfileCatalogDocument("csharp", "CSharp", "Old standard.", "standards/CSharp.md"),
            ],
            [
                new ProfileCatalogDocument("mediator", "Mediator", "Old architecture.", "architectures/Mediator.md"),
            ],
            [
                new ProfileCatalogDocument("skill-template", "Skill Template", "Template.", "templates/skill-template.md"),
            ],
            [
                new ProfileCatalogDocument("web-api", "Web API", "Old project type.", "project-types/Web-API.md"),
            ],
            [
                new ProfileCatalogDocument("repo-central", "Repo Central", "Storage.", "feature-storage/repo-central.md"),
            ],
            [
                new SupportedTerminalClient(
                    "codex",
                    "Codex",
                    "OpenAI Codex.",
                    new TerminalClientOperation("Scan with Codex.", "codex", ["exec"]),
                    new TerminalClientOperation("Open Codex.", "codex", [])),
            ],
            true,
            SituationScale.Sprint,
            SituationStorage.Database,
            "mongodb://localhost:27017/CustomDevCraft");
        DevCraftProfileConfiguration source = new(
            [
                new ProfileCatalogDocument("CREATE-API", "Create API", "New skill.", "skills/Create-API-v2.md"),
                new ProfileCatalogDocument("create-worker", "Create Worker", "Worker skill.", "skills/Create-Worker.md"),
            ],
            [
                new ProfileCatalogDocument("csharp", "CSharp", "New standard.", "standards/CSharp-v2.md"),
            ],
            [
                new ProfileCatalogDocument("mediator", "Mediator", "New architecture.", "architectures/Mediator-v2.md"),
            ],
            [
                new ProfileCatalogDocument("ignored-template", "Ignored Template", "Template.", "templates/ignored.md"),
            ],
            [
                new ProfileCatalogDocument("web-api", "Web API", "New project type.", "project-types/Web-API-v2.md"),
                new ProfileCatalogDocument("console-app", "Console App", "Console project type.", "project-types/Console-App.md"),
            ],
            [],
            []);

        (DevCraftProfileConfiguration configuration, CatalogMergeResult result) = CatalogMergeCommand.Merge(target, source);

        Assert.Equal(2, result.Added);
        Assert.Equal(4, result.Updated);
        Assert.Equal(0, result.Skipped);
        Assert.Equal("New skill.", configuration.Skills[0].Description);
        Assert.Equal("skills/Create-API-v2.md", configuration.Skills[0].Path);
        Assert.Equal("create-worker", configuration.Skills[1].Slug);
        Assert.Equal("New standard.", configuration.Standards[0].Description);
        Assert.Equal("New architecture.", configuration.Architectures[0].Description);
        Assert.Equal("New project type.", configuration.ProjectTypes[0].Description);
        Assert.Equal("console-app", configuration.ProjectTypes[1].Slug);
        Assert.Single(configuration.Templates);
        Assert.Equal("skill-template", configuration.Templates[0].Slug);
        Assert.Single(configuration.SupportedClients);
        Assert.True(configuration.SituationEnabled);
        Assert.Equal(SituationScale.Sprint, configuration.SituationScale);
        Assert.Equal(SituationStorage.Database, configuration.SituationStorage);
        Assert.Equal("mongodb://localhost:27017/CustomDevCraft", configuration.SituationConnection);
    }

    [Fact]
    public void RunMergesFileIntoProfileConfigureJson()
    {
        using TestDirectory root = new();
        using TestDirectory sourceRoot = new();
        string profile = Path.Combine(root.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        DevCraftProfileConfigurationWriter.Write(
            profile,
            new DevCraftProfileConfiguration(
                [
                    new ProfileCatalogDocument("existing-skill", "Existing Skill", "Old.", "skills/Existing.md"),
                ],
                [],
                [],
                [],
                [],
                [],
                [],
                true,
                SituationScale.Sprint,
                SituationStorage.Database,
                "mongodb://localhost:27017/DevCraftCustom"));
        string sourcePath = Path.Combine(sourceRoot.Path, "catalog.json");
        Directory.CreateDirectory(Path.Combine(sourceRoot.Path, "skills"));
        Directory.CreateDirectory(Path.Combine(sourceRoot.Path, "standards"));
        Directory.CreateDirectory(Path.Combine(sourceRoot.Path, "architectures"));
        Directory.CreateDirectory(Path.Combine(sourceRoot.Path, "project-types"));
        File.WriteAllText(Path.Combine(sourceRoot.Path, "skills", "Existing-v2.md"), "# Existing Skill");
        File.WriteAllText(Path.Combine(sourceRoot.Path, "skills", "New.md"), "# New Skill");
        File.WriteAllText(Path.Combine(sourceRoot.Path, "standards", "Standard-One.md"), "# Standard One");
        File.WriteAllText(Path.Combine(sourceRoot.Path, "architectures", "Architecture-One.md"), "# Architecture One");
        File.WriteAllText(Path.Combine(sourceRoot.Path, "project-types", "Project-Type-One.md"), "# Project Type One");
        DevCraftProfileConfigurationWriter.Write(
            sourceRoot.Path,
            new DevCraftProfileConfiguration(
                [
                    new ProfileCatalogDocument("existing-skill", "Existing Skill", "New.", "skills/Existing-v2.md"),
                    new ProfileCatalogDocument("new-skill", "New Skill", "Added.", "skills/New.md"),
                ],
                [
                    new ProfileCatalogDocument("standard-one", "Standard One", "Added standard.", "standards/Standard-One.md"),
                ],
                [
                    new ProfileCatalogDocument("architecture-one", "Architecture One", "Added architecture.", "architectures/Architecture-One.md"),
                ],
                [],
                [
                    new ProfileCatalogDocument("project-type-one", "Project Type One", "Added project type.", "project-types/Project-Type-One.md"),
                ],
                [],
                []));
        File.Move(Path.Combine(sourceRoot.Path, "configure.json"), sourcePath);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));

        CatalogMergeCommand.Run(context, [sourcePath]);

        DevCraftProfileConfiguration configuration = ProfileConfigurationReader.Read(profile);
        Assert.Contains(configuration.Skills, document =>
            document.Slug == "existing-skill" &&
            document.Description == "New." &&
            document.Path == "skills/Existing-v2.md");
        Assert.Contains(configuration.Skills, document => document.Slug == "new-skill");
        Assert.Contains(configuration.Standards, document => document.Slug == "standard-one");
        Assert.Contains(configuration.Architectures, document => document.Slug == "architecture-one");
        Assert.Contains(configuration.ProjectTypes, document => document.Slug == "project-type-one");
        Assert.True(configuration.SituationEnabled);
        Assert.Equal(SituationScale.Sprint, configuration.SituationScale);
        Assert.Equal(SituationStorage.Database, configuration.SituationStorage);
        Assert.Equal("mongodb://localhost:27017/DevCraftCustom", configuration.SituationConnection);
    }

    [Fact]
    public void RunSkipsEntriesWhenReferencedFilesDoNotExist()
    {
        using TestDirectory root = new();
        using TestDirectory sourceRoot = new();
        string profile = Path.Combine(root.Path, ".DevCraft");
        Directory.CreateDirectory(profile);
        DevCraftProfileConfigurationWriter.Write(
            profile,
            new DevCraftProfileConfiguration([], [], [], [], [], [], []));
        Directory.CreateDirectory(Path.Combine(sourceRoot.Path, "skills"));
        File.WriteAllText(Path.Combine(sourceRoot.Path, "skills", "Present.md"), "# Present Skill");
        string sourcePath = Path.Combine(sourceRoot.Path, "catalog.json");
        DevCraftProfileConfigurationWriter.Write(
            sourceRoot.Path,
            new DevCraftProfileConfiguration(
                [
                    new ProfileCatalogDocument("present-skill", "Present Skill", "Exists.", "skills/Present.md"),
                    new ProfileCatalogDocument("missing-skill", "Missing Skill", "Does not exist.", "skills/Missing.md"),
                ],
                [
                    new ProfileCatalogDocument("missing-standard", "Missing Standard", "Does not exist.", "standards/Missing.md"),
                ],
                [],
                [],
                [],
                [],
                []));
        File.Move(Path.Combine(sourceRoot.Path, "configure.json"), sourcePath);
        StartupContext context = new(root.Path, profile, Path.Combine(profile, "soul.md"));

        CatalogMergeCommand.Run(context, [sourcePath]);

        DevCraftProfileConfiguration configuration = ProfileConfigurationReader.Read(profile);
        Assert.Contains(configuration.Skills, document => document.Slug == "present-skill");
        Assert.DoesNotContain(configuration.Skills, document => document.Slug == "missing-skill");
        Assert.DoesNotContain(configuration.Standards, document => document.Slug == "missing-standard");
    }
}
