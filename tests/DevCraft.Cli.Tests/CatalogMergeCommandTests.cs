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
            "repo-central",
            [
                new SupportedTerminalClient(
                    "codex",
                    "Codex",
                    "OpenAI Codex.",
                    new TerminalClientOperation("Scan with Codex.", "codex", ["exec"]),
                    new TerminalClientOperation("Open Codex.", "codex", [])),
            ]);
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
            "system-central",
            []);

        (DevCraftProfileConfiguration configuration, CatalogMergeResult result) = CatalogMergeCommand.Merge(target, source);

        Assert.Equal(2, result.Added);
        Assert.Equal(4, result.Updated);
        Assert.Equal("New skill.", configuration.Skills[0].Description);
        Assert.Equal("skills/Create-API-v2.md", configuration.Skills[0].Path);
        Assert.Equal("create-worker", configuration.Skills[1].Slug);
        Assert.Equal("New standard.", configuration.Standards[0].Description);
        Assert.Equal("New architecture.", configuration.Architectures[0].Description);
        Assert.Equal("New project type.", configuration.ProjectTypes[0].Description);
        Assert.Equal("console-app", configuration.ProjectTypes[1].Slug);
        Assert.Single(configuration.Templates);
        Assert.Equal("skill-template", configuration.Templates[0].Slug);
        Assert.Equal("repo-central", configuration.SelectedFeatureStorage);
        Assert.Single(configuration.SupportedClients);
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
                "repo-central",
                []));
        string sourcePath = Path.Combine(sourceRoot.Path, "catalog.json");
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
                "repo-central",
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
    }
}
