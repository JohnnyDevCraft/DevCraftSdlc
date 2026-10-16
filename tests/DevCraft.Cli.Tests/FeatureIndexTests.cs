using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class FeatureIndexTests
{
    private const string Key = "key-1";
    private static readonly ProjectProfile Profile = new("Code Arc", "Purpose.", "Description.");
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Load_SystemCentralConfigWithFeatures_MergesThenStrips()
    {
        using Fixture fixture = new();
        fixture.WriteConfig([Feature("legacy", "f-legacy", FeatureStorageMode.SystemCentral)]);

        FeatureIndex index = fixture.Load();

        Assert.Contains(index.Features, feature => feature.FolderName == "f-legacy");
        Assert.Empty(ProjectDevCraftConfigurationReader.Read(fixture.Repo)!.Features);
        Assert.DoesNotContain("\"Features\"", File.ReadAllText(ProjectDevCraftConfigurationStore.PathFor(fixture.Repo)));
        Assert.Contains(index.Notices, notice => notice.Contains("Commit", StringComparison.Ordinal));
    }

    [Fact]
    public void Load_LegacyFeatureMissingCentrally_IsAddedNotLost()
    {
        using Fixture fixture = new();
        fixture.WriteCentral(Project([Feature("central", "f-central", FeatureStorageMode.SystemCentral)]));
        fixture.WriteConfig([Feature("only-in-branch", "f-branch", FeatureStorageMode.SystemCentral)]);

        fixture.Load();

        Assert.Equal(["f-central", "f-branch"], fixture.CentralProject().Features.Select(feature => feature.FolderName));
    }

    [Fact]
    public void Load_LegacyFeatureConflict_CentralWins()
    {
        using Fixture fixture = new();
        fixture.WriteCentral(Project([Feature("shared", "f-shared", FeatureStorageMode.SystemCentral) with { Name = "Central Name", Status = "Complete" }]));
        fixture.WriteConfig([Feature("shared", "f-shared", FeatureStorageMode.SystemCentral) with { Name = "Stale Name", Status = "Discovery" }]);

        fixture.Load();

        DevCraftFeature feature = Assert.Single(fixture.CentralProject().Features);
        Assert.Equal("Central Name", feature.Name);
        Assert.Equal("Complete", feature.Status);
    }

    [Fact]
    public void Load_RepoCentralFeatures_StayInConfig_CentralStatusWins()
    {
        using Fixture fixture = new();
        fixture.WriteCentral(Project([Feature("local", "f-local", FeatureStorageMode.RepoCentral) with { Status = "Complete" }]));
        fixture.WriteConfig(
        [
            Feature("local", "f-local", FeatureStorageMode.RepoCentral) with { Status = "Implementation" },
            Feature("central", "f-central", FeatureStorageMode.SystemCentral),
        ]);

        fixture.Load();

        DevCraftFeature kept = Assert.Single(ProjectDevCraftConfigurationReader.Read(fixture.Repo)!.Features);
        Assert.Equal("f-local", kept.FolderName);
        Assert.Equal("Complete", kept.Status);
    }

    [Fact]
    public void Load_RepoCentralFeature_RepositoryOwnsNameWorkItemBranch()
    {
        using Fixture fixture = new();
        fixture.WriteCentral(Project([Feature("local", "f-local", FeatureStorageMode.RepoCentral) with { Name = "Old", WorkItemId = "1", Branch = "old" }]));
        fixture.WriteConfig([Feature("local", "f-local", FeatureStorageMode.RepoCentral) with { Name = "New", WorkItemId = "2", Branch = "new" }]);

        fixture.Load();

        DevCraftFeature central = Assert.Single(fixture.CentralProject().Features);
        Assert.Equal(("New", "2", "new"), (central.Name, central.WorkItemId, central.Branch));
    }

    [Fact]
    public void Load_StripKeepsProjectKeyStorageAndProfile()
    {
        using Fixture fixture = new();
        fixture.WriteConfig([Feature("legacy", "f-legacy", FeatureStorageMode.SystemCentral)]);

        fixture.Load();

        ProjectDevCraftConfiguration configuration = ProjectDevCraftConfigurationReader.Read(fixture.Repo)!;
        Assert.Equal(Key, configuration.ProjectKey);
        Assert.Equal(FeatureStorageMode.SystemCentral, configuration.SelectedFeatureStorage);
        Assert.Equal(Profile, configuration.ProjectProfile);
    }

    [Fact]
    public void Load_UnchangedConfig_IsNotRewritten()
    {
        using Fixture fixture = new();
        fixture.WriteConfig([]);
        string path = ProjectDevCraftConfigurationStore.PathFor(fixture.Repo);
        File.SetLastWriteTimeUtc(path, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        FeatureIndex index = fixture.Load();

        Assert.Equal(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), File.GetLastWriteTimeUtc(path));
        Assert.Empty(index.Notices);
    }

    [Fact]
    public void Load_RecordsWorkingCopy_AndLeavesOtherCopiesUntouched()
    {
        using Fixture fixture = new();
        WorkingCopy other = new("/repos/Active Work 1/ProjectCodeArc", "story/1", Now.AddDays(-1));
        fixture.WriteCentral(Project([]) with { WorkingCopies = [other] });
        fixture.WriteConfig([]);

        fixture.Load(branch: "story/2");

        IReadOnlyList<WorkingCopy> copies = fixture.CentralProject().WorkingCopies;
        Assert.Equal(2, copies.Count);
        Assert.Equal(other, copies[0]);
        Assert.Equal(new WorkingCopy(fixture.Repo, "story/2", Now), copies[1]);
    }

    [Fact]
    public void Load_ExistingCopy_UpdatesBranchAndLastSeen()
    {
        using Fixture fixture = new();
        fixture.WriteCentral(Project([]) with { WorkingCopies = [new WorkingCopy(fixture.Repo, "old", Now.AddDays(-3))] });
        fixture.WriteConfig([]);

        fixture.Load(branch: "new");

        Assert.Equal(new WorkingCopy(fixture.Repo, "new", Now), Assert.Single(fixture.CentralProject().WorkingCopies));
    }

    [Fact]
    public void Load_SamePathDifferentCase_IsOneCopy_OnMacAndWindows()
    {
        if (OperatingSystem.IsLinux())
        {
            return;
        }

        using Fixture fixture = new();
        fixture.WriteCentral(Project([]) with { WorkingCopies = [new WorkingCopy(fixture.Repo.ToUpperInvariant(), null, null)] });
        fixture.WriteConfig([]);

        fixture.Load();

        Assert.Single(fixture.CentralProject().WorkingCopies);
    }

    [Fact]
    public void Load_MissingCopyFolder_IsNeverRemovedAutomatically()
    {
        using Fixture fixture = new();
        WorkingCopy gone = new("/repos/does-not-exist/ProjectCodeArc", "story/9", Now.AddYears(-1));
        fixture.WriteCentral(Project([]) with { WorkingCopies = [gone] });
        fixture.WriteConfig([]);

        fixture.Load();

        Assert.Contains(gone, fixture.CentralProject().WorkingCopies);
    }

    [Fact]
    public void Load_RefreshesRepoNameAndRemote()
    {
        using Fixture fixture = new();
        fixture.WriteCentral(Project([]) with { RepositoryName = "stale", RepositoryRemote = null });
        fixture.WriteConfig([]);

        fixture.Load(remote: "git@github.com:Org/ProjectCodeArc.git");

        SystemCentralProject project = fixture.CentralProject();
        Assert.Equal("ProjectCodeArc", project.RepositoryName);
        Assert.Equal("github.com/Org/ProjectCodeArc", project.RepositoryRemote);
    }

    [Fact]
    public void Load_NoRemote_UsesFolderNameForRepoName()
    {
        using Fixture fixture = new();
        fixture.WriteConfig([]);

        fixture.Load();

        Assert.Equal(Path.GetFileName(fixture.Repo), fixture.CentralProject().RepositoryName);
        Assert.Null(fixture.CentralProject().RepositoryRemote);
    }

    [Fact]
    public void Load_UnknownKeyMatchingRemote_Yes_AdoptsKey_RewritesOnlyProjectKey()
    {
        using Fixture fixture = new();
        fixture.WriteCentral(Project([Feature("shared", "f-shared", FeatureStorageMode.SystemCentral)]) with
        {
            RepositoryRemote = "github.com/Org/ProjectCodeArc",
        });
        fixture.WriteConfig([], key: "fresh-key");
        FakeConsoleInteraction console = new([], ["Yes"]);

        FeatureIndex index = fixture.Load(remote: "https://github.com/Org/ProjectCodeArc.git", console: console);

        Assert.Equal(Key, ProjectDevCraftConfigurationReader.Read(fixture.Repo)!.ProjectKey);
        Assert.Equal(Key, index.Configuration.ProjectKey);
        Assert.Contains(index.Features, feature => feature.FolderName == "f-shared");
        Assert.Single(SystemCentralProjectsStore.Read(fixture.Profile).Projects);
        Assert.Contains(console.SelectTitles, title => title.Contains("This clone looks like Code Arc. Use its project key?", StringComparison.Ordinal));
        Assert.Equal(["No", "Yes"], console.SelectChoices[0]);
        Assert.Contains(index.Notices, notice => notice.Contains("Commit", StringComparison.Ordinal));
    }

    [Fact]
    public void Load_UnknownKeyMatchingRemote_No_CreatesProject_NotAskedAgain()
    {
        using Fixture fixture = new();
        fixture.WriteCentral(Project([]) with { RepositoryRemote = "github.com/Org/ProjectCodeArc" });
        fixture.WriteConfig([], key: "fresh-key");
        FakeConsoleInteraction first = new([], ["No"]);

        fixture.Load(remote: "git@github.com:Org/ProjectCodeArc.git", console: first);
        FakeConsoleInteraction second = new([]);
        fixture.Load(remote: "git@github.com:Org/ProjectCodeArc.git", console: second);

        Assert.Equal("fresh-key", ProjectDevCraftConfigurationReader.Read(fixture.Repo)!.ProjectKey);
        Assert.Equal(2, SystemCentralProjectsStore.Read(fixture.Profile).Projects.Count);
        Assert.Single(first.SelectTitles);
        Assert.Empty(second.SelectTitles);
    }

    [Fact]
    public void Load_UnknownKeyTwoMatches_OffersPickerWithNone()
    {
        using Fixture fixture = new();
        fixture.WriteCentral(
            Project([]) with { RepositoryRemote = "github.com/Org/ProjectCodeArc" },
            Project([]) with { ProjectKey = "key-2", Name = "Code Arc Fork", RepositoryRemote = "github.com/Org/ProjectCodeArc" });
        fixture.WriteConfig([], key: "fresh-key");
        FakeConsoleInteraction console = new([], ["Code Arc Fork"]);

        fixture.Load(remote: "git@github.com:Org/ProjectCodeArc.git", console: console);

        Assert.Equal(["Code Arc", "Code Arc Fork", "None"], console.SelectChoices[0]);
        Assert.Equal("key-2", ProjectDevCraftConfigurationReader.Read(fixture.Repo)!.ProjectKey);
    }

    [Fact]
    public void Load_UnknownKeyNoRemote_CreatesProjectWithoutPrompt()
    {
        using Fixture fixture = new();
        fixture.WriteCentral(Project([]) with { RepositoryRemote = "github.com/Org/ProjectCodeArc" });
        fixture.WriteConfig([], key: "fresh-key");
        FakeConsoleInteraction console = new([]);

        fixture.Load(console: console);

        Assert.Empty(console.SelectTitles);
        Assert.Contains(SystemCentralProjectsStore.Read(fixture.Profile).Projects, project => project.ProjectKey == "fresh-key");
    }

    [Fact]
    public void Save_SystemCentralFeature_NeverWrittenToConfig()
    {
        using Fixture fixture = new();
        fixture.WriteConfig([]);
        FeatureIndex index = fixture.Load();

        index.Save(Feature("new", "f-new", FeatureStorageMode.SystemCentral));

        Assert.Empty(ProjectDevCraftConfigurationReader.Read(fixture.Repo)!.Features);
        Assert.Contains(fixture.CentralProject().Features, feature => feature.FolderName == "f-new");
    }

    [Fact]
    public void Save_RepoCentralFeature_WrittenToBoth()
    {
        using Fixture fixture = new();
        fixture.WriteConfig([]);
        FeatureIndex index = fixture.Load();

        FeatureIndex saved = index.Save(Feature("local", "f-local", FeatureStorageMode.RepoCentral) with { Branch = "b" });

        Assert.Equal("b", Assert.Single(ProjectDevCraftConfigurationReader.Read(fixture.Repo)!.Features).Branch);
        Assert.Equal("b", Assert.Single(fixture.CentralProject().Features).Branch);
        Assert.Equal("b", Assert.Single(saved.Features).Branch);
    }

    [Fact]
    public void Save_ReplacesByFolderName_AndKeepsOrder()
    {
        using Fixture fixture = new();
        fixture.WriteCentral(Project([Feature("a", "f-a", FeatureStorageMode.SystemCentral), Feature("b", "f-b", FeatureStorageMode.SystemCentral)]));
        fixture.WriteConfig([]);
        FeatureIndex index = fixture.Load();

        index.Save(index.Features[0] with { Name = "Renamed A" });

        Assert.Equal(["Renamed A", "b"], fixture.CentralProject().Features.Select(feature => feature.Name));
    }

    [Fact]
    public void Save_MovingToSystemCentral_RemovesFromConfig()
    {
        using Fixture fixture = new();
        fixture.WriteConfig([Feature("local", "f-local", FeatureStorageMode.RepoCentral)]);
        FeatureIndex index = fixture.Load();

        index.Save(index.Features[0] with { StorageType = FeatureStorageMode.SystemCentral });

        Assert.Empty(ProjectDevCraftConfigurationReader.Read(fixture.Repo)!.Features);
        Assert.Equal(FeatureStorageMode.SystemCentral, Assert.Single(fixture.CentralProject().Features).StorageType);
    }

    [Fact]
    public void Load_BlankProjectKey_LeavesProjectsJsonAlone()
    {
        using Fixture fixture = new();
        fixture.WriteConfig([Feature("local", "f-local", FeatureStorageMode.RepoCentral)], key: "");

        FeatureIndex index = fixture.Load();

        Assert.Empty(SystemCentralProjectsStore.Read(fixture.Profile).Projects);
        Assert.Equal("f-local", Assert.Single(index.Features).FolderName);
    }

    [Fact]
    public void Load_ReportsCurrentCopyGitInfo()
    {
        using Fixture fixture = new();
        fixture.WriteConfig([]);

        FeatureIndex index = fixture.Load(branch: "story/7");

        Assert.Equal("story/7", index.CurrentCopy.Branch);
        Assert.True(index.CurrentCopy.IsRepository);
    }

    private static SystemCentralProject Project(IReadOnlyList<DevCraftFeature> features)
    {
        return new SystemCentralProject(Key, "Code Arc", "code-arc", "ProjectCodeArc", null, [], features);
    }

    private static DevCraftFeature Feature(string slug, string folder, string storage)
    {
        return new DevCraftFeature(slug, slug, "Pending.", folder, storage, null, "Discovery");
    }

    private sealed class Fixture : IDisposable
    {
        private readonly TestDirectory root = new();

        public Fixture()
        {
            Repo = Path.Combine(root.Path, "Active Work 2", "ProjectCodeArc");
            Profile = Path.Combine(root.Path, ".DevCraft");
            Directory.CreateDirectory(Repo);
            Directory.CreateDirectory(Profile);
        }

        public string Repo { get; }

        public string Profile { get; }

        public void WriteConfig(IReadOnlyList<DevCraftFeature> features, string key = Key)
        {
            ProjectDevCraftConfigurationStore.Write(
                Repo,
                new ProjectDevCraftConfiguration(key, FeatureStorageMode.SystemCentral, "features.json", FeatureIndexTests.Profile, features));
        }

        public void WriteCentral(params SystemCentralProject[] projects)
        {
            SystemCentralProjectsStore.Write(Profile, new SystemCentralProjectsIndex(projects));
        }

        public SystemCentralProject CentralProject()
        {
            string key = ProjectDevCraftConfigurationReader.Read(Repo)!.ProjectKey;

            return SystemCentralProjectsStore.Read(Profile).Projects.Single(project => project.ProjectKey == key);
        }

        public FeatureIndex Load(string? branch = "main", string? remote = null, FakeConsoleInteraction? console = null)
        {
            FakeGitRunner git = new FakeGitRunner().On("symbolic-ref --short -q HEAD", exitCode: branch is null ? 1 : 0, output: branch ?? string.Empty);
            git.On("remote get-url origin", exitCode: remote is null ? 2 : 0, output: remote ?? string.Empty);
            StartupContext context = new(Repo, Profile, Path.Combine(Profile, "soul.md"));

            return FeatureIndex.Load(
                context,
                ProjectDevCraftConfigurationReader.Read(Repo)!,
                git,
                console ?? new FakeConsoleInteraction([]),
                new FixedTimeProvider(Now));
        }

        public void Dispose()
        {
            root.Dispose();
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }
    }
}
