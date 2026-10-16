using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class ProjectDevCraftConfigurationStoreTests
{
    private static readonly ProjectProfile Profile = new("Sample", "Purpose.", "Description.");

    [Fact]
    public void Write_OmitsFeatures_WhenEmpty()
    {
        using TestDirectory root = new();

        ProjectDevCraftConfigurationStore.Write(root.Path, Configuration([]));

        string json = File.ReadAllText(ProjectDevCraftConfigurationStore.PathFor(root.Path));
        Assert.DoesNotContain("\"Features\"", json);
        Assert.Contains("\"ProjectKey\": \"key-1\"", json);
    }

    [Fact]
    public void Read_MissingFeatures_ExposesEmptyList()
    {
        using TestDirectory root = new();
        ProjectDevCraftConfigurationStore.Write(root.Path, Configuration([]));

        ProjectDevCraftConfiguration configuration = ProjectDevCraftConfigurationReader.Read(root.Path)!;

        Assert.NotNull(configuration.Features);
        Assert.Empty(configuration.Features);
    }

    [Fact]
    public void Read_Beta20ConfigWithoutWorkItemOrBranch_Loads()
    {
        using TestDirectory root = new();
        Directory.CreateDirectory(Path.Combine(root.Path, ".devcraft"));
        File.WriteAllText(ProjectDevCraftConfigurationStore.PathFor(root.Path), """
            {
              "ProjectKey": "key-1",
              "SelectedFeatureStorage": "system-central",
              "FeaturesIndexPath": "features.json",
              "ProjectProfile": { "Name": "Sample", "Purpose": "P", "Description": "D" },
              "Features": [
                { "Name": "Old", "Slug": "old", "ShortDescription": "Old one.", "FolderName": "f1", "StorageType": "system-central", "ExternalReference": null, "Status": "Complete" }
              ]
            }
            """);

        DevCraftFeature feature = Assert.Single(ProjectDevCraftConfigurationReader.Read(root.Path)!.Features);

        Assert.Equal("old", feature.Slug);
        Assert.Null(feature.WorkItemId);
        Assert.Null(feature.Branch);
        Assert.Equal("Complete", feature.Status);
    }

    [Fact]
    public void RepoCentralFeature_WorkItemIdAndBranch_RoundTrip()
    {
        using TestDirectory root = new();
        DevCraftFeature feature = new(
            "Local",
            "local",
            "Local feature.",
            "f1",
            FeatureStorageMode.RepoCentral,
            "https://github.com/org/repo/issues/57",
            "Discovery",
            "#57",
            "john/story/57-local");

        ProjectDevCraftConfigurationStore.Write(root.Path, Configuration([feature]));

        Assert.Equal(feature, Assert.Single(ProjectDevCraftConfigurationReader.Read(root.Path)!.Features));
    }

    private static ProjectDevCraftConfiguration Configuration(IReadOnlyList<DevCraftFeature> features)
    {
        return new ProjectDevCraftConfiguration("key-1", FeatureStorageMode.SystemCentral, "features.json", Profile, features);
    }
}
