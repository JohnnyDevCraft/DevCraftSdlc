using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class GitToolingConfigurationCommandTests
{
    private const string SetupIdentity = "Setup GIT Configuration (Email & Name)";
    private const string SetupVariables = "Setup Branch Variables";
    private const string SetupTemplate = "Setup Branch Template";

    [Fact]
    public void MenuListsTheThreeSetupOptions()
    {
        using TestDirectory profile = new();
        FakeConsoleInteraction console = new([], ["Back"]);

        GitToolingConfigurationCommand.Run(Context(profile), console, new FakeGitRunner());

        Assert.Equal([SetupIdentity, SetupVariables, SetupTemplate, "Back"], console.SelectChoices[0]);
    }

    [Fact]
    public void SetupIdentityPrefillsCurrentGlobalValuesAndWritesBoth()
    {
        using TestDirectory profile = new();
        FakeConsoleInteraction console = new([], [SetupIdentity, "Back"]);
        console.EnqueueFieldEdit(new Dictionary<string, string> { ["Email"] = "new@example.com", ["Name"] = "New Name" });
        FakeGitRunner git = new FakeGitRunner()
            .On("config --global user.email", output: "old@example.com\n")
            .On("config --global user.name", output: "Old Name\n");

        GitToolingConfigurationCommand.Run(Context(profile), console, git);

        Assert.Equal(["old@example.com", "Old Name"], console.FieldEditFields[0].Select(field => field.InitialValue));
        Assert.Contains("config --global user.email new@example.com", git.Calls);
        Assert.Contains("config --global user.name New Name", git.Calls);
        Assert.StartsWith("Git identity set: New Name <new@example.com>.", console.SelectTitles.Last(), StringComparison.Ordinal);
    }

    [Fact]
    public void SetupIdentityCancelWritesNothing()
    {
        using TestDirectory profile = new();
        FakeConsoleInteraction console = new([], [SetupIdentity, "Back"]);
        console.EnqueueFieldEdit(new Dictionary<string, string> { ["Email"] = "x@example.com", ["Name"] = "X" }, saved: false);
        FakeGitRunner git = new FakeGitRunner().On("config --global user.email", exitCode: 1).On("config --global user.name", exitCode: 1);

        GitToolingConfigurationCommand.Run(Context(profile), console, git);

        Assert.Equal(["", ""], console.FieldEditFields[0].Select(field => field.InitialValue));
        Assert.DoesNotContain(git.Calls, call => call.StartsWith("config --global user.email ", StringComparison.Ordinal));
        Assert.StartsWith("Git configuration was not changed.", console.SelectTitles.Last(), StringComparison.Ordinal);
    }

    [Fact]
    public void AddVariablePersistsToProfileConfiguration()
    {
        using TestDirectory profile = new();
        FakeConsoleInteraction console = new([], [SetupVariables, "Add Variable", "Back", "Back"]);
        console.EnqueueFieldEdit(new Dictionary<string, string> { ["Name"] = "adUser", ["Value"] = "jdoe" });

        GitToolingConfigurationCommand.Run(Context(profile), console, new FakeGitRunner());

        Assert.Equal("jdoe", ProfileConfigurationReader.Read(profile.Path).GitTooling.BranchVariables["adUser"]);
        Assert.Contains("adUser = jdoe", console.SelectChoices[2]);
    }

    [Theory]
    [InlineData("Title")]
    [InlineData("workitem")]
    [InlineData("FeatureType")]
    public void AddVariableRejectsBuiltInTokens(string name)
    {
        using TestDirectory profile = new();
        FakeConsoleInteraction console = new([], [SetupVariables, "Add Variable", "Back", "Back"]);
        console.EnqueueFieldEdit(new Dictionary<string, string> { ["Name"] = name, ["Value"] = "x" });

        GitToolingConfigurationCommand.Run(Context(profile), console, new FakeGitRunner());

        Assert.Empty(ProfileConfigurationReader.Read(profile.Path).GitTooling.BranchVariables);
        Assert.Contains(console.SelectTitles, title => title.Contains("is a built-in token", StringComparison.Ordinal));
    }

    [Fact]
    public void AddVariableRejectsDuplicateAndInvalidNames()
    {
        using TestDirectory profile = new();
        WriteVariables(profile, new Dictionary<string, string> { ["adUser"] = "jdoe" });
        FakeConsoleInteraction console = new([], [SetupVariables, "Add Variable", "Add Variable", "Back", "Back"]);
        console.EnqueueFieldEdit(new Dictionary<string, string> { ["Name"] = "ADUSER", ["Value"] = "x" });
        console.EnqueueFieldEdit(new Dictionary<string, string> { ["Name"] = "bad name", ["Value"] = "x" });

        GitToolingConfigurationCommand.Run(Context(profile), console, new FakeGitRunner());

        Assert.Equal("jdoe", Assert.Single(ProfileConfigurationReader.Read(profile.Path).GitTooling.BranchVariables).Value);
        Assert.Contains(console.SelectTitles, title => title.Contains("already exists", StringComparison.Ordinal));
        Assert.Contains(console.SelectTitles, title => title.Contains("cannot contain spaces or braces", StringComparison.Ordinal));
    }

    [Fact]
    public void EditVariablePrefillsAndRenames()
    {
        using TestDirectory profile = new();
        WriteVariables(profile, new Dictionary<string, string> { ["adUser"] = "jdoe" });
        FakeConsoleInteraction console = new([], [SetupVariables, "adUser = jdoe", "Edit Variable", "Back", "Back"]);
        console.EnqueueFieldEdit(new Dictionary<string, string> { ["Name"] = "user", ["Value"] = "INPUT" });

        GitToolingConfigurationCommand.Run(Context(profile), console, new FakeGitRunner());

        Assert.Equal(["adUser", "jdoe"], console.FieldEditFields[0].Select(field => field.InitialValue));
        KeyValuePair<string, string> variable = Assert.Single(ProfileConfigurationReader.Read(profile.Path).GitTooling.BranchVariables);
        Assert.Equal(("user", "INPUT"), (variable.Key, variable.Value));
    }

    [Fact]
    public void RemoveVariableDeletesIt()
    {
        using TestDirectory profile = new();
        WriteVariables(profile, new Dictionary<string, string> { ["adUser"] = "jdoe", ["Team"] = "core" });
        FakeConsoleInteraction console = new([], [SetupVariables, "adUser = jdoe", "Remove Variable", "Back", "Back"]);

        GitToolingConfigurationCommand.Run(Context(profile), console, new FakeGitRunner());

        Assert.Equal(["Team"], ProfileConfigurationReader.Read(profile.Path).GitTooling.BranchVariables.Keys);
    }

    [Fact]
    public void SetupTemplatePrefillsAndPersists()
    {
        using TestDirectory profile = new();
        DevCraftProfileConfigurationWriter.Write(
            profile.Path,
            new DevCraftProfileConfiguration([], [], [], [], [], [], [])
            {
                GitTooling = new GitToolingSettings { BranchTemplate = "{Title}" },
            });
        FakeConsoleInteraction console = new([], [SetupTemplate, "Back"]);
        console.EnqueueValueEdit("{adUser}/{FeatureType}/{WorkItem}-{Title}");

        GitToolingConfigurationCommand.Run(Context(profile), console, new FakeGitRunner());

        Assert.Equal("{Title}", console.ValueEditInitialValues[0]);
        Assert.Equal("{adUser}/{FeatureType}/{WorkItem}-{Title}", ProfileConfigurationReader.Read(profile.Path).GitTooling.BranchTemplate);
        Assert.Contains("asked for when a branch is created: adUser", console.SelectTitles.Last(), StringComparison.Ordinal);
    }

    private static StartupContext Context(TestDirectory profile)
    {
        return new StartupContext(profile.Path, profile.Path, Path.Combine(profile.Path, "soul.md"));
    }

    private static void WriteVariables(TestDirectory profile, Dictionary<string, string> variables)
    {
        DevCraftProfileConfigurationWriter.Write(
            profile.Path,
            new DevCraftProfileConfiguration([], [], [], [], [], [], [])
            {
                GitTooling = new GitToolingSettings { BranchVariables = variables },
            });
    }
}
