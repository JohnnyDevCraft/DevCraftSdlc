using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class ProfileDocumentCatalogBuilderTests
{
    [Fact]
    public void BuildSkipsReadmeAndTemplateFiles()
    {
        using TestDirectory root = new();
        string profile = Path.Combine(root.Path, ".DevCraft");
        string skills = Path.Combine(profile, "skills");
        Directory.CreateDirectory(skills);
        File.WriteAllText(Path.Combine(skills, "README.md"), "# Skills");
        File.WriteAllText(Path.Combine(skills, "_template.md"), "# Skill: Template");
        File.WriteAllText(Path.Combine(skills, "Operator-Questions.md"), "# Skill: Operator Questions");

        IReadOnlyList<ProfileCatalogDocument> documents = ProfileDocumentCatalogBuilder.Build(profile, "skills");

        ProfileCatalogDocument document = Assert.Single(documents);
        Assert.Equal("operator-questions", document.Slug);
        Assert.Equal("Operator Questions", document.Name);
    }
}
