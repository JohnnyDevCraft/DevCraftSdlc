using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class FeatureMoverTests
{
    [Fact]
    public void Move_RepoToSystem_CopiesVerifiesDeletesSource_UpdatesIndexes()
    {
        using Fixture fixture = new();
        DevCraftFeature feature = fixture.Local("alpha", ("spec.md", "# Alpha"), ("research.md", "notes"));
        FeatureIndex index = fixture.Load();

        FeatureMoveResult result = FeatureMover.Move(fixture.Context, index, feature, FeatureStorageMode.SystemCentral);

        Assert.True(result.Moved);
        string target = Path.Combine(fixture.Profile, "features", feature.FolderName);
        Assert.Equal("# Alpha", File.ReadAllText(Path.Combine(target, "spec.md")));
        Assert.Equal("notes", File.ReadAllText(Path.Combine(target, "research.md")));
        Assert.False(Directory.Exists(Path.Combine(fixture.Repo, ".devcraft", "features", feature.FolderName)));
        Assert.Empty(ProjectDevCraftConfigurationReader.Read(fixture.Repo)!.Features);
        Assert.Equal(FeatureStorageMode.SystemCentral, Assert.Single(result.Index.Features).StorageType);
    }

    [Fact]
    public void Move_SystemToRepo_AddsToConfigFeatures()
    {
        using Fixture fixture = new();
        DevCraftFeature feature = fixture.Central("beta", ("spec.md", "# Beta"));
        FeatureIndex index = fixture.Load();

        FeatureMoveResult result = FeatureMover.Move(fixture.Context, index, feature, FeatureStorageMode.RepoCentral);

        Assert.True(result.Moved);
        Assert.Equal("# Beta", File.ReadAllText(Path.Combine(fixture.Repo, ".devcraft", "features", feature.FolderName, "spec.md")));
        Assert.False(Directory.Exists(Path.Combine(fixture.Profile, "features", feature.FolderName)));
        Assert.Equal(FeatureStorageMode.RepoCentral, Assert.Single(ProjectDevCraftConfigurationReader.Read(fixture.Repo)!.Features).StorageType);
    }

    [Fact]
    public void Move_VerifyFails_SourceIntact_TargetCleaned_Reported()
    {
        using Fixture fixture = new();
        DevCraftFeature feature = fixture.Local("alpha", ("spec.md", "# Alpha"));
        FeatureIndex index = fixture.Load();

        FeatureMoveResult result = FeatureMover.Move(
            fixture.Context,
            index,
            feature,
            FeatureStorageMode.SystemCentral,
            (source, destination) => File.WriteAllText(destination, "corrupted"));

        Assert.False(result.Moved);
        Assert.Equal("spec.md did not copy correctly.", result.Error);
        Assert.Equal("# Alpha", File.ReadAllText(Path.Combine(fixture.Repo, ".devcraft", "features", feature.FolderName, "spec.md")));
        Assert.False(Directory.Exists(Path.Combine(fixture.Profile, "features", feature.FolderName)));
        Assert.Equal(FeatureStorageMode.RepoCentral, Assert.Single(ProjectDevCraftConfigurationReader.Read(fixture.Repo)!.Features).StorageType);
    }

    [Fact]
    public void Move_TargetFolderAlreadyExists_Refused()
    {
        using Fixture fixture = new();
        DevCraftFeature feature = fixture.Local("alpha", ("spec.md", "# Alpha"));
        Directory.CreateDirectory(Path.Combine(fixture.Profile, "features", feature.FolderName));
        File.WriteAllText(Path.Combine(fixture.Profile, "features", feature.FolderName, "spec.md"), "other");
        FeatureIndex index = fixture.Load();

        FeatureMoveResult result = FeatureMover.Move(fixture.Context, index, feature, FeatureStorageMode.SystemCentral);

        Assert.False(result.Moved);
        Assert.Equal("the target folder already exists.", result.Error);
        Assert.Equal("other", File.ReadAllText(Path.Combine(fixture.Profile, "features", feature.FolderName, "spec.md")));
    }

    [Fact]
    public void Move_EmptyFeatureFolder_Moves()
    {
        using Fixture fixture = new();
        DevCraftFeature feature = fixture.Local("alpha");
        FeatureIndex index = fixture.Load();

        FeatureMoveResult result = FeatureMover.Move(fixture.Context, index, feature, FeatureStorageMode.SystemCentral);

        Assert.True(result.Moved);
        Assert.True(Directory.Exists(Path.Combine(fixture.Profile, "features", feature.FolderName)));
    }

    private sealed class Fixture : IDisposable
    {
        private readonly TestDirectory root = new();
        private readonly List<DevCraftFeature> local = [];
        private readonly List<DevCraftFeature> central = [];

        public Fixture()
        {
            Repo = Path.Combine(root.Path, "repo");
            Profile = Path.Combine(root.Path, ".DevCraft");
            Context = new StartupContext(Repo, Profile, Path.Combine(Profile, "soul.md"));
        }

        public string Repo { get; }

        public string Profile { get; }

        public StartupContext Context { get; }

        public DevCraftFeature Local(string slug, params (string Name, string Content)[] files)
        {
            DevCraftFeature feature = new(slug, slug, "Pending.", $"folder-{slug}", FeatureStorageMode.RepoCentral, null);
            local.Add(feature);
            WriteFiles(Path.Combine(Repo, ".devcraft", "features", feature.FolderName), files);

            return feature;
        }

        public DevCraftFeature Central(string slug, params (string Name, string Content)[] files)
        {
            DevCraftFeature feature = new(slug, slug, "Pending.", $"folder-{slug}", FeatureStorageMode.SystemCentral, null);
            central.Add(feature);
            WriteFiles(Path.Combine(Profile, "features", feature.FolderName), files);

            return feature;
        }

        public FeatureIndex Load()
        {
            ProjectDevCraftConfiguration configuration = new("key-1", FeatureStorageMode.RepoCentral, "features.json", new ProjectProfile("Sample", "P", "D"), local);
            ProjectDevCraftConfigurationStore.Write(Repo, configuration);
            SystemCentralProjectsStore.Write(Profile, new SystemCentralProjectsIndex([new SystemCentralProject("key-1", "Sample", "sample", "repo", null, [], central)]));

            return FeatureIndex.Load(Context, configuration, new FakeGitRunner(), new FakeConsoleInteraction([]));
        }

        public void Dispose()
        {
            root.Dispose();
        }

        private static void WriteFiles(string folder, (string Name, string Content)[] files)
        {
            Directory.CreateDirectory(folder);

            foreach ((string name, string content) in files)
            {
                File.WriteAllText(Path.Combine(folder, name), content);
            }
        }
    }
}
