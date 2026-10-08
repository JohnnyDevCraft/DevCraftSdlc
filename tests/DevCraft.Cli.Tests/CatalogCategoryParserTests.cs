using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class CatalogCategoryParserTests
{
    [Fact]
    public void ParseReturnsAllForMissingCategory()
    {
        Assert.Equal(CatalogCategory.All, CatalogCategoryParser.Parse(null));
    }

    [Theory]
    [InlineData("skills", CatalogCategory.Skills)]
    [InlineData("standards", CatalogCategory.Standards)]
    [InlineData("architectures", CatalogCategory.Architectures)]
    [InlineData("templates", CatalogCategory.Templates)]
    [InlineData("project-types", CatalogCategory.ProjectTypes)]
    [InlineData("feature-storage", CatalogCategory.FeatureStorageTypes)]
    [InlineData("clients", CatalogCategory.SupportedClients)]
    public void ParseReturnsRequestedCategory(string value, CatalogCategory expected)
    {
        Assert.Equal(expected, CatalogCategoryParser.Parse(value));
    }
}
