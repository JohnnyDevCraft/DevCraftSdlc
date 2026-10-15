using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class GitFeatureActionsTests
{
    private const string CommitMessageResponse = """{ "success": true, "summary": "Login fix.", "message": "Fix login redirect\n\nKeep the return URL." }""";

    private static readonly DevCraftFeature Feature = new("Fix login page", "fix-login-page", "Pending.", "folder", "system-central", "AB#1234");

    [Fact]
    public void CommitCodeStagesAsksAgentForMessageCommitsAndPushesWithUpstream()
    {
        using TestDirectory root = new();
        FakeGitRunner git = DirtyRepository("feature/x")
            .On("diff --cached --stat", output: " src/a.cs | 2 +-\n")
            .On("diff --cached", output: "+changed line\n")
            .On("rev-parse --abbrev-ref --symbolic-full-name @{u}", exitCode: 128, error: "no upstream");
        FakeRepositoryDocumentAgent agent = new() { Response = CommitMessageResponse };
        File.WriteAllText(Path.Combine(root.Path, "soul.md"), "- Default terminal AI agent: Claude AI\n");
        DevCraftProfileConfigurationWriter.Write(root.Path, new DevCraftProfileConfiguration([], [], [], [], [], [], SupportedTerminalClientCatalog.Create()));

        string notice = Actions(root, new FakeConsoleInteraction([]), git, agent).CommitCode();

        string prompt = Assert.Single(agent.Prompts);
        Assert.Contains("feature/x", prompt);
        Assert.Contains("src/a.cs | 2 +-", prompt);
        Assert.Contains("+changed line", prompt);
        Assert.Equal("claude-code", Assert.Single(agent.Clients).Slug);
        Assert.True(git.Calls.IndexOf("add -A") < git.Calls.IndexOf("commit -m Fix login redirect\n\nKeep the return URL."));
        Assert.Contains("push -u origin feature/x", git.Calls);
        Assert.Contains("Committed to feature/x: Fix login redirect", notice);
        Assert.Contains("Pushed feature/x to origin and set it as the upstream.", notice);
    }

    [Fact]
    public void CommitCodeTruncatesLongDiffs()
    {
        using TestDirectory root = new();
        FakeGitRunner git = DirtyRepository("main")
            .On("diff --cached --stat", output: " big.txt | 1 +\n")
            .On("diff --cached", output: new string('x', 20_000));
        FakeRepositoryDocumentAgent agent = new() { Response = CommitMessageResponse };

        Actions(root, new FakeConsoleInteraction([]), git, agent).CommitCode();

        string prompt = Assert.Single(agent.Prompts);
        Assert.Contains("truncated", prompt);
        Assert.True(prompt.Length < 15_000);
    }

    [Fact]
    public void CommitCodeWithCleanTreeCallsNoAgentAndPushesOnlyWhenAhead()
    {
        using TestDirectory root = new();
        FakeGitRunner git = CleanRepository("main").On("rev-list --count @{u}..HEAD", output: "2\n");
        FakeRepositoryDocumentAgent agent = new();

        string notice = Actions(root, new FakeConsoleInteraction([]), git, agent).CommitCode();

        Assert.Empty(agent.Prompts);
        Assert.DoesNotContain("add -A", git.Calls);
        Assert.Contains("push", git.Calls);
        Assert.Contains("Nothing to commit.", notice);
        Assert.Contains("Pushed main to origin.", notice);
    }

    [Fact]
    public void CommitCodeWithCleanTreeAndNothingAheadDoesNotPush()
    {
        using TestDirectory root = new();
        FakeGitRunner git = CleanRepository("main").On("rev-list --count @{u}..HEAD", output: "0\n");

        string notice = Actions(root, new FakeConsoleInteraction([]), git, new FakeRepositoryDocumentAgent()).CommitCode();

        Assert.DoesNotContain(git.Calls, call => call.StartsWith("push", StringComparison.Ordinal));
        Assert.Equal("Nothing to commit. main is up to date with origin.", notice);
    }

    [Fact]
    public void CommitCodeAgentFailureCommitsNothing()
    {
        using TestDirectory root = new();
        FakeGitRunner git = DirtyRepository("main").On("diff --cached --stat", output: " a | 1 +\n");
        FakeRepositoryDocumentAgent agent = new() { Response = """{ "success": false, "summary": "No idea.", "message": "" }""" };

        string notice = Actions(root, new FakeConsoleInteraction([]), git, agent).CommitCode();

        Assert.DoesNotContain(git.Calls, call => call.StartsWith("commit", StringComparison.Ordinal));
        Assert.DoesNotContain(git.Calls, call => call.StartsWith("push", StringComparison.Ordinal));
        Assert.Equal("Commit Code failed: No idea. The changes are staged and nothing was committed.", notice);
    }

    [Fact]
    public void CommitCodeOutsideRepositoryReportsNotice()
    {
        using TestDirectory root = new();
        FakeGitRunner git = new FakeGitRunner().On("rev-parse --is-inside-work-tree", exitCode: 128, error: "fatal: not a git repository");

        string notice = Actions(root, new FakeConsoleInteraction([]), git, new FakeRepositoryDocumentAgent()).CommitCode();

        Assert.Equal("This folder is not a git repository.", notice);
        Assert.Single(git.Calls);
    }

    [Fact]
    public void CommitCodeOnDetachedHeadReportsNotice()
    {
        using TestDirectory root = new();
        FakeGitRunner git = new FakeGitRunner().On("symbolic-ref --short -q HEAD", exitCode: 1);

        string notice = Actions(root, new FakeConsoleInteraction([]), git, new FakeRepositoryDocumentAgent()).CommitCode();

        Assert.Equal("HEAD is detached. Switch to a branch first.", notice);
    }

    [Fact]
    public void SwitchAndPullListsLocalAndOriginBranchesAndPulls()
    {
        using TestDirectory root = new();
        FakeGitRunner git = CleanRepository("main")
            .On("for-each-ref --format=%(refname:short) refs/heads refs/remotes/origin", output: "develop\nmain\norigin\norigin/HEAD\norigin/develop\norigin/release\n");
        FakeConsoleInteraction console = new([], ["release"]);

        string? notice = Actions(root, console, git, new FakeRepositoryDocumentAgent()).SwitchAndPull();

        Assert.Equal(["develop", "release", "Back"], console.SelectChoices[0]);
        Assert.True(git.Calls.IndexOf("switch release") < git.Calls.IndexOf("pull --no-edit"));
        Assert.Equal("Switched to release and pulled the latest commits from origin.", notice);
    }

    [Fact]
    public void SwitchAndPullWithoutUpstreamSkipsPull()
    {
        using TestDirectory root = new();
        FakeGitRunner git = CleanRepository("main")
            .On("for-each-ref --format=%(refname:short) refs/heads refs/remotes/origin", output: "local-only\nmain\n")
            .On("rev-parse --abbrev-ref --symbolic-full-name @{u}", exitCode: 128);
        FakeConsoleInteraction console = new([], ["local-only"]);

        string? notice = Actions(root, console, git, new FakeRepositoryDocumentAgent()).SwitchAndPull();

        Assert.DoesNotContain("pull --no-edit", git.Calls);
        Assert.Equal("Switched to local-only. It has no upstream on origin, so there was nothing to pull.", notice);
    }

    [Fact]
    public void SwitchAndPullWithChangesCanBeCancelled()
    {
        using TestDirectory root = new();
        FakeGitRunner git = DirtyRepository("main");
        FakeConsoleInteraction console = new([], ["Cancel"]);

        string? notice = Actions(root, console, git, new FakeRepositoryDocumentAgent()).SwitchAndPull();

        Assert.Equal(["Commit Code first", "Cancel"], console.SelectChoices[0]);
        Assert.DoesNotContain(git.Calls, call => call.StartsWith("switch", StringComparison.Ordinal));
        Assert.Equal("Switch and Pull cancelled. You have uncommitted changes.", notice);
    }

    [Fact]
    public void SwitchAndPullWithChangesCommitsFirstWhenChosen()
    {
        using TestDirectory root = new();
        FakeGitRunner git = DirtyRepository("main")
            .On("diff --cached --stat", output: " a | 1 +\n")
            .On("for-each-ref --format=%(refname:short) refs/heads refs/remotes/origin", output: "develop\nmain\n");
        FakeConsoleInteraction console = new([], ["Commit Code first", "develop"]);

        string? notice = Actions(root, console, git, new FakeRepositoryDocumentAgent { Response = CommitMessageResponse }).SwitchAndPull();

        Assert.True(git.Calls.IndexOf("push") < git.Calls.IndexOf("switch develop"));
        Assert.Contains("Committed to main", notice);
        Assert.Contains("Switched to develop", notice);
    }

    [Fact]
    public void CreateBranchFromTemplateUsesFeatureDefaultsVariablesAndFeatureType()
    {
        using TestDirectory root = new();
        WriteProfile(root, "{adUser}/{FeatureType}/{WorkItem}-{Title}-{Team}", new Dictionary<string, string> { ["adUser"] = "jdoe", ["Team"] = "INPUT", ["Unused"] = "INPUT" });
        FakeGitRunner git = CleanRepository("main").On("show-ref --verify --quiet refs/heads/jdoe/bug/1234-Fix-login-page-core", exitCode: 1);
        FakeConsoleInteraction console = new([], ["Use branch template", "bug"]);
        console.EnqueueFieldEdit(new Dictionary<string, string> { ["WorkItem"] = "1234", ["Title"] = "Fix login page", ["Team"] = "core" });

        string? notice = Actions(root, console, git, new FakeRepositoryDocumentAgent()).CreateBranch(Feature);

        Assert.Equal(["Use branch template", "Enter a custom branch name", "Back"], console.SelectChoices[0]);
        Assert.Equal(["enabler", "bug", "story"], console.SelectChoices[1]);
        Assert.Equal(["WorkItem", "Title", "Team"], console.FieldEditFields[0].Select(field => field.Key));
        Assert.Equal(["AB#1234", "Fix login page", ""], console.FieldEditFields[0].Select(field => field.InitialValue));
        Assert.Equal("jdoe/bug/1234-Fix-login-page-core", console.ValueEditInitialValues[0]);
        Assert.Contains("check-ref-format --branch jdoe/bug/1234-Fix-login-page-core", git.Calls);
        Assert.Contains("switch -c jdoe/bug/1234-Fix-login-page-core", git.Calls);
        Assert.Equal("Created branch jdoe/bug/1234-Fix-login-page-core from main and switched to it.", notice);
    }

    [Fact]
    public void CreateBranchPromptsForTemplateTokensWithoutVariables()
    {
        using TestDirectory root = new();
        WriteProfile(root, "{Sprint}/{Title}", new Dictionary<string, string>());
        FakeGitRunner git = CleanRepository("main").On("show-ref --verify --quiet refs/heads/s12/Thing", exitCode: 1);
        FakeConsoleInteraction console = new([], ["Use branch template"]);
        console.EnqueueFieldEdit(new Dictionary<string, string> { ["Sprint"] = "s12", ["Title"] = "Thing" });

        string? notice = Actions(root, console, git, new FakeRepositoryDocumentAgent()).CreateBranch(Feature);

        Assert.Equal(["Sprint", "Title"], console.FieldEditFields[0].Select(field => field.Key));
        Assert.Equal("Created branch s12/Thing from main and switched to it.", notice);
    }

    [Fact]
    public void CreateBranchWithoutTemplateExplainsHowToSetOne()
    {
        using TestDirectory root = new();
        FakeConsoleInteraction console = new([], ["Use branch template"]);

        string? notice = Actions(root, console, CleanRepository("main"), new FakeRepositoryDocumentAgent()).CreateBranch(Feature);

        Assert.Equal("No branch template is set. Set one in Configure DevCraft > Configure GIT Tooling > Setup Branch Template.", notice);
    }

    [Fact]
    public void CreateBranchCustomInvalidNameCreatesNothing()
    {
        using TestDirectory root = new();
        FakeGitRunner git = CleanRepository("main").On("check-ref-format --branch bad..name", exitCode: 1);
        FakeConsoleInteraction console = new([], ["Enter a custom branch name"]);
        console.EnqueueValueEdit("bad..name");

        string? notice = Actions(root, console, git, new FakeRepositoryDocumentAgent()).CreateBranch(Feature);

        Assert.DoesNotContain(git.Calls, call => call.StartsWith("switch", StringComparison.Ordinal));
        Assert.Equal("bad..name is not a valid branch name. Branch was not created.", notice);
    }

    [Fact]
    public void CreateBranchRejectsExistingBranch()
    {
        using TestDirectory root = new();
        FakeConsoleInteraction console = new([], ["Enter a custom branch name"]);
        console.EnqueueValueEdit("develop");

        string? notice = Actions(root, console, CleanRepository("main"), new FakeRepositoryDocumentAgent()).CreateBranch(Feature);

        Assert.Equal("Branch develop already exists. Branch was not created.", notice);
    }

    [Fact]
    public void CreateBranchCancelledFormCreatesNothing()
    {
        using TestDirectory root = new();
        WriteProfile(root, "{Title}", new Dictionary<string, string>());
        FakeGitRunner git = CleanRepository("main");
        FakeConsoleInteraction console = new([], ["Use branch template"]);
        console.EnqueueFieldEdit(new Dictionary<string, string> { ["Title"] = "x" }, saved: false);

        string? notice = Actions(root, console, git, new FakeRepositoryDocumentAgent()).CreateBranch(Feature);

        Assert.Equal("Branch was not created.", notice);
        Assert.DoesNotContain(git.Calls, call => call.StartsWith("switch", StringComparison.Ordinal));
    }

    [Fact]
    public void MergeFromCommitsSwitchesPullsReturnsAndMerges()
    {
        using TestDirectory root = new();
        FakeGitRunner git = DirtyRepository("feature/x")
            .On("diff --cached --stat", output: " a | 1 +\n")
            .On("for-each-ref --format=%(refname:short) refs/heads refs/remotes/origin", output: "feature/x\nmain\n");
        FakeConsoleInteraction console = new([], ["main"]);

        string? notice = Actions(root, console, git, new FakeRepositoryDocumentAgent { Response = CommitMessageResponse }).MergeFrom();

        List<string> sequence = git.Calls.Where(call =>
            call.StartsWith("commit", StringComparison.Ordinal) ||
            call.StartsWith("switch", StringComparison.Ordinal) ||
            call.StartsWith("pull", StringComparison.Ordinal) ||
            call.StartsWith("merge", StringComparison.Ordinal) ||
            call.StartsWith("push", StringComparison.Ordinal)).ToList();
        Assert.Equal(["commit -m Fix login redirect\n\nKeep the return URL.", "switch main", "pull --no-edit", "switch feature/x", "merge --no-edit main"], sequence);
        Assert.Equal(["main", "Back"], console.SelectChoices[0]);
        Assert.Contains("Committed to feature/x", notice);
        Assert.Contains("Merged main into feature/x.", notice);
    }

    [Fact]
    public void MergeFromReportsConflictsAndStaysOnOriginalBranch()
    {
        using TestDirectory root = new();
        FakeGitRunner git = CleanRepository("feature/x")
            .On("for-each-ref --format=%(refname:short) refs/heads refs/remotes/origin", output: "feature/x\nmain\n")
            .On("merge --no-edit main", exitCode: 1, output: "CONFLICT (content)")
            .On("diff --name-only --diff-filter=U", output: "src/a.cs\nsrc/b.cs\n");
        FakeConsoleInteraction console = new([], ["main"]);

        string? notice = Actions(root, console, git, new FakeRepositoryDocumentAgent()).MergeFrom();

        Assert.Equal("switch feature/x", git.Calls.Last(call => call.StartsWith("switch", StringComparison.Ordinal)));
        Assert.DoesNotContain(git.Calls, call => call.Contains("--abort", StringComparison.Ordinal) || call.StartsWith("reset", StringComparison.Ordinal));
        Assert.Equal("Merging main into feature/x stopped with conflicts in: src/a.cs, src/b.cs. Resolve them and commit.", notice);
    }

    [Fact]
    public void MergeFromReturnsToOriginalBranchWhenPullFails()
    {
        using TestDirectory root = new();
        FakeGitRunner git = CleanRepository("feature/x")
            .On("for-each-ref --format=%(refname:short) refs/heads refs/remotes/origin", output: "feature/x\nmain\n")
            .On("pull --no-edit", exitCode: 1, error: "fatal: Need to specify how to reconcile divergent branches.");
        FakeConsoleInteraction console = new([], ["main"]);

        string? notice = Actions(root, console, git, new FakeRepositoryDocumentAgent()).MergeFrom();

        Assert.Equal("switch feature/x", git.Calls.Last());
        Assert.Equal("Pull of main failed: fatal: Need to specify how to reconcile divergent branches. Returned to feature/x; nothing was merged.", notice);
    }

    private static GitFeatureActions Actions(TestDirectory root, FakeConsoleInteraction console, FakeGitRunner git, FakeRepositoryDocumentAgent agent)
    {
        StartupContext context = new(root.Path, root.Path, Path.Combine(root.Path, "soul.md"));
        return new GitFeatureActions(context, console, git, agent);
    }

    private static FakeGitRunner CleanRepository(string branch)
    {
        return new FakeGitRunner().On("symbolic-ref --short -q HEAD", output: $"{branch}\n");
    }

    private static FakeGitRunner DirtyRepository(string branch)
    {
        return CleanRepository(branch)
            .On("status --porcelain", output: " M src/a.cs\n")
            .On("status --porcelain", output: string.Empty);
    }

    private static void WriteProfile(TestDirectory root, string template, Dictionary<string, string> variables)
    {
        DevCraftProfileConfigurationWriter.Write(
            root.Path,
            new DevCraftProfileConfiguration([], [], [], [], [], [], [])
            {
                GitTooling = new GitToolingSettings { BranchTemplate = template, BranchVariables = variables },
            });
    }
}
