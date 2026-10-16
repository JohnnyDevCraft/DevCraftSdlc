using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class FeatureStorageSelectorTests
{
    private const string MoveTitle = "Move existing features to system-central? Select none to leave them where they are.";

    [Fact]
    public void ChangeToSystemCentral_OffersRepoCentralActiveFeatures()
    {
        using Fixture fixture = new(FeatureStorageMode.RepoCentral,
            Local("Alpha", "alpha", "Discovery"),
            Local("Beta", "beta", "Implementation"));
        FakeConsoleInteraction console = new([], ["system-central"]);
        console.EnqueueSelectMany("Alpha (alpha)");

        fixture.Change(console);

        Assert.Equal(MoveTitle, Assert.Single(console.SelectManyTitles));
        Assert.Equal(["Alpha (alpha)", "Beta (beta)"], console.SelectManyChoices[0]);
        Assert.Equal(FeatureStorageMode.SystemCentral, fixture.CentralFeature("alpha").StorageType);
        Assert.Equal(FeatureStorageMode.RepoCentral, fixture.CentralFeature("beta").StorageType);
        Assert.Contains(console.StatusMessages, message => message == "Moved to system-central: Alpha. Not moved: Beta.");
    }

    [Fact]
    public void ChangeToRepoCentral_OffersSystemCentralActiveFeatures()
    {
        using Fixture fixture = new(FeatureStorageMode.SystemCentral);
        fixture.WriteCentralFeatures(Central("Gamma", "gamma", "Discovery"));
        FakeConsoleInteraction console = new([], ["repo-central"]);
        console.EnqueueSelectMany("Gamma (gamma)");

        fixture.Change(console);

        Assert.Equal(["Gamma (gamma)"], console.SelectManyChoices[0]);
        Assert.Equal("gamma", Assert.Single(ProjectDevCraftConfigurationReader.Read(fixture.Repo)!.Features).Slug);
    }

    [Fact]
    public void CompleteFeatures_NotOffered()
    {
        using Fixture fixture = new(FeatureStorageMode.RepoCentral, Local("Done", "done", "Complete"), Local("Open", "open", "Discovery"));
        FakeConsoleInteraction console = new([], ["system-central"]);

        fixture.Change(console);

        Assert.Equal(["Open (open)"], console.SelectManyChoices[0]);
    }

    [Fact]
    public void SelectNone_MovesNothing_ModeSaved()
    {
        using Fixture fixture = new(FeatureStorageMode.RepoCentral, Local("Alpha", "alpha", "Discovery"));
        FakeConsoleInteraction console = new([], ["system-central"]);

        fixture.Change(console);

        Assert.Equal(FeatureStorageMode.SystemCentral, ProjectDevCraftConfigurationReader.Read(fixture.Repo)!.SelectedFeatureStorage);
        Assert.Equal(FeatureStorageMode.RepoCentral, fixture.CentralFeature("alpha").StorageType);
        Assert.Contains("No features were moved.", console.StatusMessages);
    }

    [Fact]
    public void ExternalTarget_ShowsLaterFeatureNotice_NoMove()
    {
        using Fixture fixture = new(FeatureStorageMode.RepoCentral, Local("Alpha", "alpha", "Discovery"));
        FakeConsoleInteraction console = new([], ["github-issue"]);

        fixture.Change(console);

        Assert.Empty(console.SelectManyTitles);
        Assert.Contains(
            "Moving existing features to github-issue will come in a later feature. Existing features stay where they are; new features will use github-issue.",
            console.StatusMessages);
        Assert.Equal("github-issue", ProjectDevCraftConfigurationReader.Read(fixture.Repo)!.SelectedFeatureStorage);
    }

    [Fact]
    public void UseProfileDefault_ResolvesTargetForOffer()
    {
        using Fixture fixture = new(FeatureStorageMode.RepoCentral, Local("Alpha", "alpha", "Discovery"));
        FakeConsoleInteraction console = new([], ["Use Profile Default"]);

        fixture.Change(console);

        Assert.Equal(MoveTitle, Assert.Single(console.SelectManyTitles));
    }

    [Fact]
    public void NothingToOffer_NoPrompt()
    {
        using Fixture fixture = new(FeatureStorageMode.RepoCentral);
        FakeConsoleInteraction console = new([], ["system-central"]);

        fixture.Change(console);

        Assert.Empty(console.SelectManyTitles);
    }

    private static DevCraftFeature Local(string name, string slug, string status)
    {
        return new DevCraftFeature(name, slug, "Pending.", $"folder-{slug}", FeatureStorageMode.RepoCentral, null, status);
    }

    private static DevCraftFeature Central(string name, string slug, string status)
    {
        return new DevCraftFeature(name, slug, "Pending.", $"folder-{slug}", FeatureStorageMode.SystemCentral, null, status);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly TestDirectory root = new();

        public Fixture(string storage, params DevCraftFeature[] repositoryFeatures)
        {
            Repo = Path.Combine(root.Path, "repo");
            Profile = Path.Combine(root.Path, ".DevCraft");
            Directory.CreateDirectory(Profile);
            DevCraftProfileConfigurationWriter.Write(
                Profile,
                new DevCraftProfileConfiguration([], [], [], [], [], [], []) { DefaultFeatureStorage = FeatureStorageMode.SystemCentral });
            ProjectDevCraftConfigurationStore.Write(
                Repo,
                new ProjectDevCraftConfiguration("key-1", storage, "features.json", new ProjectProfile("Sample", "P", "D"), repositoryFeatures));

            foreach (DevCraftFeature feature in repositoryFeatures)
            {
                string folder = Path.Combine(Repo, ".devcraft", "features", feature.FolderName);
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "spec.md"), $"# {feature.Name}");
            }
        }

        public string Repo { get; }

        public string Profile { get; }

        public void WriteCentralFeatures(params DevCraftFeature[] features)
        {
            SystemCentralProjectsStore.Write(
                Profile,
                new SystemCentralProjectsIndex([new SystemCentralProject("key-1", "Sample", "sample", "repo", null, [], features)]));

            foreach (DevCraftFeature feature in features)
            {
                string folder = Path.Combine(Profile, "features", feature.FolderName);
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "spec.md"), $"# {feature.Name}");
            }
        }

        public void Change(FakeConsoleInteraction console)
        {
            StartupContext context = new(Repo, Profile, Path.Combine(Profile, "soul.md"));
            FeatureStorageSelector.Change(context, console, ProjectDevCraftConfigurationReader.Read(Repo)!, new FakeGitRunner());
        }

        public DevCraftFeature CentralFeature(string slug)
        {
            return Assert.Single(SystemCentralProjectsStore.Read(Profile).Projects).Features.Single(feature => feature.Slug == slug);
        }

        public void Dispose()
        {
            root.Dispose();
        }
    }
}
