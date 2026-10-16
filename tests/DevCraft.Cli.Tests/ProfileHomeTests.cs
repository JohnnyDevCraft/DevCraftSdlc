using System.Security.Cryptography;
using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

// The operator ran DevCraft in ~ by accident. On case-insensitive file systems ~/.devcraft is the
// profile folder ~/.DevCraft, so DevCraft must treat ~ as the profile home, not as a project.
public sealed class ProfileHomeTests
{
    private const string Message = "This is your home folder, where your DevCraft profile lives";

    [Fact]
    public void Is_TrueForProfileParent()
    {
        using Home home = new();

        Assert.True(ProfileHome.Is(home.Context));
    }

    [Fact]
    public void Is_FalseForProjectFolderInsideHome()
    {
        using Home home = new();
        string project = Path.Combine(home.Root, "Source", "repos", "CodeArc");
        Directory.CreateDirectory(project);

        Assert.False(ProfileHome.Is(home.Context with { CurrentDirectory = project }));
    }

    [Fact]
    public void Is_FalseForUnrelatedFolder()
    {
        using Home home = new();
        using TestDirectory other = new();

        Assert.False(ProfileHome.Is(home.Context with { CurrentDirectory = other.Path }));
    }

    [Fact]
    public void StoreWrite_IntoProfileFolder_IsRefused_ProfileUnchanged()
    {
        using Home home = new();
        string before = File.ReadAllText(Path.Combine(home.Profile, "configure.json"));
        string profileParentAsProject = Path.Combine(home.Root, "fake-project");
        Directory.CreateDirectory(profileParentAsProject);
        Directory.CreateSymbolicLink(Path.Combine(profileParentAsProject, ".devcraft"), home.Profile);

        InvalidOperationException refused = Assert.Throws<InvalidOperationException>(() => ProjectDevCraftConfigurationStore.Write(
            profileParentAsProject,
            new ProjectDevCraftConfiguration("key", null, "features.json", new ProjectProfile("X", "P", "D"), [])));

        Assert.Contains("DevCraft profile folder", refused.Message, StringComparison.Ordinal);
        Assert.Equal(before, File.ReadAllText(Path.Combine(home.Profile, "configure.json")));
    }

    [Fact]
    public void Installer_IntoProfileFolder_IsRefused()
    {
        using Home home = new();
        string before = home.Fingerprint();

        Assert.Throws<InvalidOperationException>(() => DevCraftInstaller.Install(home.Root, new ProjectProfile("X", "P", "D"), home.Profile));

        Assert.Equal(before, home.Fingerprint());
    }

    [Fact]
    public void Installer_InProfileHomeWhereDevcraftIsSeparateFolder_IsRefused()
    {
        // Case-sensitive file systems (Linux): ~/.devcraft is not ~/.DevCraft, but ~ is still the profile home.
        using TestDirectory root = new();
        string profile = Path.Combine(root.Path, "profile-store");
        Directory.CreateDirectory(profile);

        Assert.Throws<InvalidOperationException>(() => DevCraftInstaller.Install(root.Path, new ProjectProfile("X", "P", "D"), profile));

        Assert.False(Directory.Exists(Path.Combine(root.Path, ".devcraft")));
    }

    [Fact]
    public void StoreWrite_NormalRepository_Allowed()
    {
        using TestDirectory repo = new();

        ProjectDevCraftConfigurationStore.Write(repo.Path, new ProjectDevCraftConfiguration("key", null, "features.json", new ProjectProfile("X", "P", "D"), []));

        Assert.True(File.Exists(ProjectDevCraftConfigurationStore.PathFor(repo.Path)));
    }

    [Fact]
    public void AccidentalRunInHome_NoQuestionsNoInstallNoThrow_ProfileUnchanged()
    {
        using Home home = new();
        string before = home.Fingerprint();
        FakeConsoleInteraction console = new([], ["Exit"]);

        bool proceed = new StartupFlow(console, ThrowingScanner(), () => [], new FakeDevCraftRepositoryUpgradeLauncher()).Run(home.Context);
        DevCraftMenuCommand.Run(home.Context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.True(proceed);
        Assert.Contains(console.StatusMessages, message => message.StartsWith(Message, StringComparison.Ordinal));
        Assert.Empty(console.ValueEditTitles);
        Assert.Empty(console.FieldEditTitles);
        Assert.DoesNotContain("Features", console.SelectChoices[0]);
        Assert.Equal(before, home.Fingerprint());
    }

    [Fact]
    public void Force_InHome_DoesNotLaunchUpgradeAgent()
    {
        using Home home = new();
        string before = home.Fingerprint();
        FakeDevCraftRepositoryUpgradeLauncher upgrade = new();
        FakeConsoleInteraction console = new([]);

        new StartupFlow(console, ThrowingScanner(), () => [], upgrade).Run(home.Context, forceInstall: true);

        Assert.Empty(upgrade.Requests);
        Assert.Contains(console.StatusMessages, message => message.StartsWith(Message, StringComparison.Ordinal));
        Assert.Equal(before, home.Fingerprint());
    }

    [Fact]
    public void Menu_InHome_IsProfileOnly()
    {
        using Home home = new();
        FakeConsoleInteraction console = new([], ["Exit"]);

        DevCraftMenuCommand.Run(home.Context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        Assert.Equal(["Logging", "Situational Conversation", "Skills", "Configure DevCraft", "Exit"], console.SelectChoices[0]);
    }

    [Fact]
    public void ConfigureDevCraft_InHome_HasNoFeatureStorageItem()
    {
        using Home home = new();
        FakeConsoleInteraction console = new([], ["Configure DevCraft", "Back", "Exit"]);

        DevCraftMenuCommand.Run(home.Context, console, new FakeDevCraftAiSessionLauncher(), new FakeFeatureAiSessionLauncher());

        IReadOnlyList<string> configure = console.SelectChoices[console.SelectTitles.IndexOf("Configure DevCraft")];
        Assert.DoesNotContain("Change Repository-Level Feature Storage", configure);
        Assert.Contains("Edit Soul", configure);
    }

    [Theory]
    [InlineData("list")]
    [InlineData("new")]
    public void FeatureCommand_InHome_Refuses(string command)
    {
        using Home home = new();
        string before = home.Fingerprint();
        FakeConsoleInteraction console = new([]);
        FakeFeatureAiSessionLauncher launcher = new();

        FeatureCommand.Run(home.Context, [command, "Oops"], console, launcher, gitRunner: new FakeGitRunner());

        Assert.Contains(console.StatusMessages, message => message.StartsWith(Message, StringComparison.Ordinal));
        Assert.Empty(launcher.Launches);
        Assert.Equal(before, home.Fingerprint());
    }

    private static IAiProjectScanner ThrowingScanner()
    {
        return new ThrowingAiProjectScanner();
    }

    private sealed class Home : IDisposable
    {
        private readonly TestDirectory root = new();

        public Home()
        {
            Profile = Path.Combine(root.Path, ".DevCraft");
            Directory.CreateDirectory(Profile);
            ProfileStructureInitializer.Ensure(Profile);
            File.WriteAllText(Path.Combine(Profile, "soul.md"), "- Default terminal AI agent: Codex");
            Context = new StartupContext(root.Path, Profile, Path.Combine(Profile, "soul.md"));
        }

        public string Root => root.Path;

        public string Profile { get; }

        public StartupContext Context { get; }

        public string Fingerprint()
        {
            return string.Join(
                "\n",
                Directory.EnumerateFileSystemEntries(Root, "*", SearchOption.AllDirectories)
                    .Where(path => !path.EndsWith(".lock", StringComparison.Ordinal))
                    .Order(StringComparer.Ordinal)
                    .Select(path => File.Exists(path)
                        ? $"{path} {Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))}"
                        : path));
        }

        public void Dispose()
        {
            root.Dispose();
        }
    }
}
