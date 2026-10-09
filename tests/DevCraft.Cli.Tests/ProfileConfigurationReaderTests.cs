using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class ProfileConfigurationReaderTests
{
    [Fact]
    public void ReadFileMigratesLegacyConfigurationToCurrentSchemaVersion()
    {
        using TestDirectory root = new();
        string path = Path.Combine(root.Path, "configure.json");
        File.WriteAllText(
            path,
            """
            {
              "Skills": [],
              "Standards": [],
              "Architectures": [],
              "Templates": [],
              "ProjectTypes": [],
              "FeatureStorageTypes": [],
              "SupportedClients": [],
              "SituationEnabled": true,
              "SituationScale": "sprint",
              "SituationStorage": "database",
              "SituationConnection": "  mongodb://localhost:27017/LegacyDevCraft  "
            }
            """);

        DevCraftProfileConfiguration configuration = ProfileConfigurationReader.ReadFile(path);

        Assert.Equal(DevCraftProfileConfiguration.CurrentSchemaVersion, configuration.SchemaVersion);
        Assert.True(configuration.SituationEnabled);
        Assert.Equal(SituationScale.Sprint, configuration.SituationScale);
        Assert.Equal(SituationStorage.Database, configuration.SituationStorage);
        Assert.Equal("mongodb://localhost:27017/LegacyDevCraft", configuration.SituationConnection);
    }

    [Fact]
    public void WriteReadCycleIsIdempotentForCurrentSchemaVersion()
    {
        using TestDirectory root = new();
        DevCraftProfileConfiguration original = new(
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            true,
            SituationScale.Sprint,
            SituationStorage.Database,
            "mongodb://localhost:27017/CurrentDevCraft");

        DevCraftProfileConfigurationWriter.Write(root.Path, original);
        DevCraftProfileConfiguration firstRead = ProfileConfigurationReader.Read(root.Path);
        DevCraftProfileConfigurationWriter.Write(root.Path, firstRead);
        DevCraftProfileConfiguration secondRead = ProfileConfigurationReader.Read(root.Path);

        Assert.Equal(firstRead.SchemaVersion, secondRead.SchemaVersion);
        Assert.Equal(firstRead.SituationEnabled, secondRead.SituationEnabled);
        Assert.Equal(firstRead.SituationScale, secondRead.SituationScale);
        Assert.Equal(firstRead.SituationStorage, secondRead.SituationStorage);
        Assert.Equal(firstRead.SituationConnection, secondRead.SituationConnection);
        Assert.Empty(secondRead.Skills);
        Assert.Empty(secondRead.Standards);
        Assert.Empty(secondRead.Architectures);
        Assert.Empty(secondRead.Templates);
        Assert.Empty(secondRead.ProjectTypes);
        Assert.Empty(secondRead.FeatureStorageTypes);
        Assert.Equal(firstRead.SupportedClients.Select(client => client.Slug), secondRead.SupportedClients.Select(client => client.Slug));
        Assert.Equal(DevCraftProfileConfiguration.CurrentSchemaVersion, secondRead.SchemaVersion);
        Assert.True(File.ReadAllText(Path.Combine(root.Path, "configure.json")).Contains("\"SchemaVersion\": 1", StringComparison.Ordinal));
    }

    [Fact]
    public void ReadFileRejectsFutureSchemaVersion()
    {
        using TestDirectory root = new();
        string path = Path.Combine(root.Path, "configure.json");
        File.WriteAllText(
            path,
            """
            {
              "SchemaVersion": 99,
              "SituationEnabled": true,
              "SituationScale": "sprint",
              "SituationStorage": "database",
              "SituationConnection": "mongodb://localhost:27017/FutureDevCraft"
            }
            """);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => ProfileConfigurationReader.ReadFile(path));
        Assert.Contains("newer than this DevCraft version supports", exception.Message);
    }
}
