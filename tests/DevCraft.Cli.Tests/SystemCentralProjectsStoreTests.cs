using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class SystemCentralProjectsStoreTests
{
    [Fact]
    public void Read_LegacyRepoLocation_BecomesSingleWorkingCopy()
    {
        using TestDirectory profile = new();
        WriteProjectsJson(profile.Path, """
            {
              "Projects": [
                {
                  "id": "key-1",
                  "name": "Code Arc",
                  "slug": "code-arc",
                  "repo-location": "/repos/Active Work 1/ProjectCodeArc",
                  "repo-name": "ProjectCodeArc",
                  "features": []
                }
              ]
            }
            """);

        SystemCentralProject project = Assert.Single(SystemCentralProjectsStore.Read(profile.Path).Projects);

        WorkingCopy copy = Assert.Single(project.WorkingCopies);
        Assert.Equal("/repos/Active Work 1/ProjectCodeArc", copy.Path);
        Assert.Null(copy.Branch);
        Assert.Null(copy.LastSeen);
        Assert.Equal("ProjectCodeArc", project.RepositoryName);
        Assert.Null(project.RepositoryRemote);
    }

    [Fact]
    public void Read_LegacyRepoLocationWithoutRepoName_DerivesNameFromPath()
    {
        using TestDirectory profile = new();
        WriteProjectsJson(profile.Path, """
            { "Projects": [ { "id": "key-1", "name": "Legacy", "slug": "legacy", "RepositoryPath": "/tmp/legacy-project", "features": [] } ] }
            """);

        SystemCentralProject project = Assert.Single(SystemCentralProjectsStore.Read(profile.Path).Projects);

        Assert.Equal("legacy-project", project.RepositoryName);
        Assert.Equal("/tmp/legacy-project", Assert.Single(project.WorkingCopies).Path);
    }

    [Fact]
    public void WorkItemId_RoundTrips_ForSystemCentralFeature()
    {
        using TestDirectory profile = new();
        DevCraftFeature feature = Feature() with { WorkItemId = "PROJ-482", ExternalReference = "https://jira.example.com/browse/PROJ-482" };

        SystemCentralProjectsStore.Write(profile.Path, Index(Project([feature])));

        DevCraftFeature read = Assert.Single(Assert.Single(SystemCentralProjectsStore.Read(profile.Path).Projects).Features);
        Assert.Equal("PROJ-482", read.WorkItemId);
        Assert.Equal("https://jira.example.com/browse/PROJ-482", read.ExternalReference);
        Assert.Contains("\"work-item-id\": \"PROJ-482\"", File.ReadAllText(SystemCentralProjectsStore.PathFor(profile.Path)));
    }

    [Fact]
    public void WorkItemId_IsNotDerivedFromExternalReference()
    {
        using TestDirectory profile = new();
        DevCraftFeature feature = Feature() with { StorageType = "ado-work-item", ExternalReference = "https://dev.azure.com/org/p/_workitems/edit/1" };

        SystemCentralProjectsStore.Write(profile.Path, Index(Project([feature])));

        Assert.Null(Assert.Single(Assert.Single(SystemCentralProjectsStore.Read(profile.Path).Projects).Features).WorkItemId);
    }

    [Fact]
    public void Branch_RoundTrips()
    {
        using TestDirectory profile = new();

        SystemCentralProjectsStore.Write(profile.Path, Index(Project([Feature() with { Branch = "john/story/1234-linking" }])));

        Assert.Equal(
            "john/story/1234-linking",
            Assert.Single(Assert.Single(SystemCentralProjectsStore.Read(profile.Path).Projects).Features).Branch);
    }

    [Fact]
    public void WorkingCopies_RoundTrip_WithIsoLastSeen()
    {
        using TestDirectory profile = new();
        DateTimeOffset seen = new(2026, 10, 16, 14, 30, 0, TimeSpan.Zero);
        SystemCentralProject project = Project([]) with
        {
            WorkingCopies =
            [
                new WorkingCopy("/repos/Active Work 1/ProjectCodeArc", "main", seen),
                new WorkingCopy("/repos/Active Work 2/ProjectCodeArc", null, null),
            ],
        };

        SystemCentralProjectsStore.Write(profile.Path, Index(project));

        SystemCentralProject read = Assert.Single(SystemCentralProjectsStore.Read(profile.Path).Projects);
        Assert.Equal(project.WorkingCopies, read.WorkingCopies);
        Assert.Contains("\"last-seen\": \"2026-10-16T14:30:00.0000000Z\"", File.ReadAllText(SystemCentralProjectsStore.PathFor(profile.Path)));
    }

    [Fact]
    public void RepoRemote_RoundTrips_AndNullWhenMissing()
    {
        using TestDirectory profile = new();

        SystemCentralProjectsStore.Write(
            profile.Path,
            Index(Project([]) with { RepositoryRemote = "github.com/Org/Repo" }, Project([]) with { ProjectKey = "key-2" }));

        IReadOnlyList<SystemCentralProject> projects = SystemCentralProjectsStore.Read(profile.Path).Projects;
        Assert.Equal("github.com/Org/Repo", projects[0].RepositoryRemote);
        Assert.Null(projects[1].RepositoryRemote);
    }

    [Fact]
    public void Write_DoesNotEmitRepoLocation()
    {
        using TestDirectory profile = new();

        SystemCentralProjectsStore.Write(profile.Path, Index(Project([])));

        string json = File.ReadAllText(SystemCentralProjectsStore.PathFor(profile.Path));
        Assert.DoesNotContain("repo-location", json);
        Assert.Contains("\"working-copies\":", json);
        Assert.Contains("\"repo-name\": \"ProjectCodeArc\"", json);
    }

    [Fact]
    public void Read_FileFromBeta20_LosesNoProjectsOrFeatures()
    {
        using TestDirectory profile = new();
        WriteProjectsJson(profile.Path, """
            {
              "Projects": [
                {
                  "id": "9dd05571-0e1b-472f-bfbe-5949f991b923",
                  "name": "DevCraft",
                  "slug": "devcraft",
                  "repo-location": "/Users/john/Source/repos/DevCraft",
                  "repo-name": "DevCraft",
                  "features": [
                    { "id": "f1", "feature-name": "One", "slug": "one", "description": "First.", "folder-name": "f1", "storage-type": "system-central", "work-item-id": null, "external-reference": null, "status": "Complete" },
                    { "id": "f2", "feature-name": "Two", "slug": "two", "description": "Second.", "folder-name": "f2", "storage-type": "repo-central", "work-item-id": null, "external-reference": null, "status": "Implementation" }
                  ]
                },
                { "id": "other", "name": "Other", "slug": "other", "repo-location": "/repos/other", "repo-name": "other", "features": [] }
              ]
            }
            """);

        SystemCentralProjectsIndex index = SystemCentralProjectsStore.Read(profile.Path);
        SystemCentralProjectsStore.Write(profile.Path, index);
        SystemCentralProjectsIndex reread = SystemCentralProjectsStore.Read(profile.Path);

        Assert.Equal(["9dd05571-0e1b-472f-bfbe-5949f991b923", "other"], reread.Projects.Select(project => project.ProjectKey));
        Assert.Equal(["one", "two"], reread.Projects[0].Features.Select(feature => feature.Slug));
        Assert.Equal(["Complete", "Implementation"], reread.Projects[0].Features.Select(feature => feature.Status));
        Assert.Equal("/Users/john/Source/repos/DevCraft", Assert.Single(reread.Projects[0].WorkingCopies).Path);
    }

    [Fact]
    public void Write_IsAtomic_OriginalIntactWhenTempLeftBehind()
    {
        using TestDirectory profile = new();
        SystemCentralProjectsStore.Write(profile.Path, Index(Project([])));
        string path = SystemCentralProjectsStore.PathFor(profile.Path);
        File.WriteAllText(path + ".tmp", "{ \"Projects\": [ { \"id\": \"half-writ");

        Assert.Equal("key-1", Assert.Single(SystemCentralProjectsStore.Read(profile.Path).Projects).ProjectKey);

        SystemCentralProjectsStore.Write(profile.Path, Index(Project([]) with { Name = "Renamed" }));

        Assert.Equal("Renamed", Assert.Single(SystemCentralProjectsStore.Read(profile.Path).Projects).Name);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void Update_TwoSequentialUpdatesFromDifferentCopies_BothSurvive()
    {
        using TestDirectory profile = new();
        SystemCentralProjectsStore.Write(profile.Path, Index(Project([])));

        SystemCentralProjectsStore.Update(profile.Path, index => Map(index, project => project with
        {
            WorkingCopies = [.. project.WorkingCopies, new WorkingCopy("/repos/Active Work 2/ProjectCodeArc", "b2", null)],
        }));
        SystemCentralProjectsStore.Update(profile.Path, index => Map(index, project => project with
        {
            Features = [Feature() with { Branch = "b1" }],
        }));

        SystemCentralProject project = Assert.Single(SystemCentralProjectsStore.Read(profile.Path).Projects);
        Assert.Equal(2, project.WorkingCopies.Count);
        Assert.Equal("b1", Assert.Single(project.Features).Branch);
    }

    [Fact]
    public void Update_Busy_WritesNothing()
    {
        using TestDirectory profile = new();
        SystemCentralProjectsStore.Write(profile.Path, Index(Project([])));
        string before = File.ReadAllText(SystemCentralProjectsStore.PathFor(profile.Path));
        using IDisposable held = ProjectsIndexLock.Acquire(profile.Path, TimeSpan.FromMilliseconds(100));

        Assert.Throws<ProjectsIndexBusyException>(() => SystemCentralProjectsStore.Update(
            profile.Path,
            index => Map(index, project => project with { Name = "Changed" }),
            TimeSpan.FromMilliseconds(100)));

        Assert.Equal(before, File.ReadAllText(SystemCentralProjectsStore.PathFor(profile.Path)));
    }

    [Fact]
    public void Read_TakesNoLock()
    {
        using TestDirectory profile = new();
        SystemCentralProjectsStore.Write(profile.Path, Index(Project([])));
        using IDisposable held = ProjectsIndexLock.Acquire(profile.Path, TimeSpan.FromMilliseconds(100));

        Assert.Single(SystemCentralProjectsStore.Read(profile.Path).Projects);
    }

    private static SystemCentralProjectsIndex Map(SystemCentralProjectsIndex index, Func<SystemCentralProject, SystemCentralProject> change)
    {
        return new SystemCentralProjectsIndex(index.Projects.Select(change).ToList());
    }

    internal static SystemCentralProject Project(IReadOnlyList<DevCraftFeature> features)
    {
        return new SystemCentralProject(
            "key-1",
            "Code Arc",
            "code-arc",
            "ProjectCodeArc",
            null,
            [new WorkingCopy("/repos/Active Work 1/ProjectCodeArc", null, null)],
            features);
    }

    private static SystemCentralProjectsIndex Index(params SystemCentralProject[] projects)
    {
        return new SystemCentralProjectsIndex(projects);
    }

    private static DevCraftFeature Feature()
    {
        return new DevCraftFeature("Linking", "linking", "Link features.", "folder-1", FeatureStorageMode.SystemCentral, null, "Discovery");
    }

    private static void WriteProjectsJson(string profileDirectory, string json)
    {
        string path = SystemCentralProjectsStore.PathFor(profileDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
    }
}
