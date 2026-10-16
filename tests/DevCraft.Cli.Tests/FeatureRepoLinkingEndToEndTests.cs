using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

// Real git, a bare origin, and clones of one repository side by side, as an operator with parallel
// working copies would have them. Prompts are scripted through FakeConsoleInteraction.
public sealed class FeatureRepoLinkingEndToEndTests
{
    private const string ProjectKey = "9a3c1f52-6f1e-4c43-9a49-0c6f2f0d8e11";
    private readonly ProcessGitRunner git = new();

    [Fact]
    public void ThreeWorkingCopies_ShareFeatures_HighlightLinkedBranch()
    {
        using TestDirectory root = new();
        string profile = Path.Combine(root.Path, ".DevCraft");
        string origin = CreateOrigin(root.Path);
        string[] copies = [Clone(root.Path, origin, 1, "story/1"), Clone(root.Path, origin, 2, "story/2"), Clone(root.Path, origin, 3, "story/3")];

        Run(copies[0], profile, ["new", "Shared", "Work"], new FakeConsoleInteraction([]));
        FakeConsoleInteraction linkInSecond = new([], ["Shared Work (shared-work)", "Set Feature to Current Branch", "Back"]);
        Run(copies[1], profile, ["list"], linkInSecond);
        FakeConsoleInteraction listInSecond = new([], ["Back"]);
        Run(copies[1], profile, ["list"], listInSecond);
        FakeConsoleInteraction listInFirst = new([], ["Back"]);
        Run(copies[0], profile, ["list"], listInFirst);
        FakeConsoleInteraction listInThird = new([], ["Back"]);
        Run(copies[2], profile, ["list"], listInThird);

        Assert.Contains("Shared Work is linked to story/2.", linkInSecond.StatusMessages);
        Assert.Equal("● Shared Work (shared-work) (current branch)", FeatureChoices(listInSecond)[2]);
        Assert.Equal("Shared Work (shared-work) → Active Work 2", FeatureChoices(listInFirst)[2]);
        Assert.Equal("Shared Work (shared-work) → Active Work 2", FeatureChoices(listInThird)[2]);
        SystemCentralProject project = Assert.Single(SystemCentralProjectsStore.Read(profile).Projects);
        Assert.Equal(ProjectKey, project.ProjectKey);
        Assert.Equal("ProjectCodeArc", project.RepositoryName);
        Assert.Equal(["story/1", "story/2", "story/3"], project.WorkingCopies.Select(copy => copy.Branch));
        Assert.Equal("story/2", Assert.Single(project.Features).Branch);

        foreach (string copy in copies)
        {
            Assert.DoesNotContain("\"Features\"", File.ReadAllText(ProjectDevCraftConfigurationStore.PathFor(copy)));
        }
    }

    [Fact]
    public void CloneWithFreshKey_AdoptsExistingProject()
    {
        using TestDirectory root = new();
        string profile = Path.Combine(root.Path, ".DevCraft");
        string origin = CreateOrigin(root.Path);
        string first = Clone(root.Path, origin, 1, "story/1");
        string stray = Clone(root.Path, origin, 2, "story/2");
        Run(first, profile, ["new", "Existing", "Work"], new FakeConsoleInteraction([]));
        string config = ProjectDevCraftConfigurationStore.PathFor(stray);
        File.WriteAllText(config, File.ReadAllText(config).Replace(ProjectKey, "00000000-0000-0000-0000-000000000002", StringComparison.Ordinal));
        string before = File.ReadAllText(config);
        FakeConsoleInteraction console = new([], ["Yes", "Back"]);

        Run(stray, profile, ["list"], console);

        Assert.Contains("This clone looks like ProjectCodeArc. Use its project key?", console.SelectTitles);
        Assert.Equal(before.Replace("00000000-0000-0000-0000-000000000002", ProjectKey, StringComparison.Ordinal), File.ReadAllText(config));
        Assert.Contains("Existing Work (existing-work)", FeatureChoices(console));
        Assert.Single(SystemCentralProjectsStore.Read(profile).Projects);
    }

    [Fact]
    public void LockHeldElsewhere_FeatureCommandReportsBusy_FileUnchanged()
    {
        using TestDirectory root = new();
        string profile = Path.Combine(root.Path, ".DevCraft");
        string origin = CreateOrigin(root.Path);
        string copy = Clone(root.Path, origin, 1, "story/1");
        Run(copy, profile, ["new", "Locked", "Work"], new FakeConsoleInteraction([]));
        string before = File.ReadAllText(SystemCentralProjectsStore.PathFor(profile));
        FakeConsoleInteraction console = new([], ["Back"]);

        Thread holder = new(() =>
        {
            using IDisposable held = ProjectsIndexLock.Acquire(profile);
            Thread.Sleep(TimeSpan.FromSeconds(3));
        });
        holder.Start();
        Thread.Sleep(200);
        Run(copy, profile, ["list"], console);
        holder.Join();

        Assert.Contains("projects.json is busy in another DevCraft session. Try again.", console.StatusMessages);
        Assert.Equal(before, File.ReadAllText(SystemCentralProjectsStore.PathFor(profile)));
    }

    private static List<string> FeatureChoices(FakeConsoleInteraction console)
    {
        return console.SelectChoices[console.SelectTitles.IndexOf("Select a feature")].ToList();
    }

    private void Run(string copy, string profile, IReadOnlyList<string> args, FakeConsoleInteraction console)
    {
        FeatureCommand.Run(
            new StartupContext(copy, profile, Path.Combine(profile, "soul.md")),
            args,
            console,
            new FakeFeatureAiSessionLauncher(),
            handoff: null,
            selectHandoff: () => new AgentHandoff(SupportedTerminalClientCatalog.Create()[0], string.Empty),
            gitRunner: git);
    }

    // A bare origin whose main branch already holds a committed System Central .devcraft/configure.json.
    private string CreateOrigin(string root)
    {
        string origin = Path.Combine(root, "origin", "ProjectCodeArc.git");
        string seed = Path.Combine(root, "seed");
        Directory.CreateDirectory(origin);
        Git(root, "-c", "init.defaultBranch=main", "init", "--bare", "--quiet", origin);
        Git(root, "-c", "init.defaultBranch=main", "init", "--quiet", seed);
        Git(seed, "config", "user.email", "test@example.com");
        Git(seed, "config", "user.name", "Test");
        Git(seed, "config", "commit.gpgsign", "false");
        ProjectDevCraftConfigurationStore.Write(
            seed,
            new ProjectDevCraftConfiguration(ProjectKey, FeatureStorageMode.SystemCentral, "features.json", new ProjectProfile("ProjectCodeArc", "P", "D"), []));
        Git(seed, "add", "-A");
        Git(seed, "commit", "--quiet", "-m", "Add DevCraft configuration");
        Git(seed, "remote", "add", "origin", origin);
        Git(seed, "push", "--quiet", "origin", "main");

        return origin;
    }

    private string Clone(string root, string origin, int number, string branch)
    {
        string copy = Path.Combine(root, $"Active Work {number}", "ProjectCodeArc");
        Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
        Git(root, "clone", "--quiet", origin, copy);
        Git(copy, "switch", "--quiet", "-c", branch);

        return copy;
    }

    private void Git(string directory, params string[] arguments)
    {
        TerminalProcessResult result = git.Run(directory, arguments);
        Assert.True(result.ExitCode == 0, $"git {string.Join(' ', arguments)} failed: {result.StandardError}");
    }
}
