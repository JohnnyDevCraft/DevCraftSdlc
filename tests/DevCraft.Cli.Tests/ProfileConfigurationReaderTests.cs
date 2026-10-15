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
        Assert.Equal(FeatureStorageMode.SystemCentral, configuration.DefaultFeatureStorage);
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
            "mongodb://localhost:27017/CurrentDevCraft")
        {
            DefaultFeatureStorage = FeatureStorageMode.RepoCentral,
        };

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
        Assert.Equal(FeatureStorageMode.RepoCentral, secondRead.DefaultFeatureStorage);
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
    [Fact]
    public void ReadFileDefaultsFeatureTypesAndEmptyGitToolingWhenMissing()
    {
        using TestDirectory root = new();
        string path = Path.Combine(root.Path, "configure.json");
        File.WriteAllText(path, """{ "SchemaVersion": 1 }""");

        DevCraftProfileConfiguration configuration = ProfileConfigurationReader.ReadFile(path);

        Assert.Equal(["enabler", "bug", "story"], configuration.FeatureTypes);
        Assert.Equal(string.Empty, configuration.GitTooling.BranchTemplate);
        Assert.Empty(configuration.GitTooling.BranchVariables);
    }

    [Fact]
    public void FeatureTypesAndGitToolingRoundTripThroughWriter()
    {
        using TestDirectory root = new();
        DevCraftProfileConfigurationWriter.Write(
            root.Path,
            new DevCraftProfileConfiguration([], [], [], [], [], [], [])
            {
                FeatureTypes = ["feature", " bug ", "feature", ""],
                GitTooling = new GitToolingSettings
                {
                    BranchTemplate = "{adUser}/{FeatureType}/{WorkItem}-{Title}",
                    BranchVariables = new Dictionary<string, string> { ["adUser"] = "jdoe", ["Team"] = "INPUT" },
                },
            });

        DevCraftProfileConfiguration configuration = ProfileConfigurationReader.Read(root.Path);

        Assert.Equal(["feature", "bug"], configuration.FeatureTypes);
        Assert.Equal("{adUser}/{FeatureType}/{WorkItem}-{Title}", configuration.GitTooling.BranchTemplate);
        Assert.Equal("jdoe", configuration.GitTooling.BranchVariables["adUser"]);
        Assert.Equal("INPUT", configuration.GitTooling.BranchVariables["Team"]);
    }

    [Fact]
    public void ProfileStructureEnsurePreservesFeatureTypesAndGitTooling()
    {
        using TestDirectory root = new();
        string profile = Path.Combine(root.Path, ".DevCraft");
        DevCraftProfileConfigurationWriter.Write(
            profile,
            new DevCraftProfileConfiguration([], [], [], [], [], [], [])
            {
                FeatureTypes = ["task"],
                GitTooling = new GitToolingSettings
                {
                    BranchTemplate = "{WorkItem}",
                    BranchVariables = new Dictionary<string, string> { ["adUser"] = "jdoe" },
                },
            });

        ProfileStructureInitializer.Ensure(profile);

        DevCraftProfileConfiguration configuration = ProfileConfigurationReader.Read(profile);
        Assert.Equal(["task"], configuration.FeatureTypes);
        Assert.Equal("{WorkItem}", configuration.GitTooling.BranchTemplate);
        Assert.Equal("jdoe", configuration.GitTooling.BranchVariables["adUser"]);
    }
}
