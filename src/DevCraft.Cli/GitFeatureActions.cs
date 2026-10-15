namespace DevCraft.Cli;

// Git actions offered from Feature actions. Only Commit Code calls an AI agent, and only to write the
// commit message; DevCraft runs every git command itself and never runs destructive recovery commands.
public sealed class GitFeatureActions(
    StartupContext context,
    IConsoleInteraction console,
    IGitRunner git,
    IRepositoryDocumentAgent agent)
{
    public const string UseTemplate = "Use branch template";
    public const string CustomName = "Enter a custom branch name";
    public const string CommitFirst = "Commit Code first";
    public const string Cancel = "Cancel";
    private const string Back = "Back";
    private const string NotCreated = "Branch was not created.";
    private const string DetachedHead = "HEAD is detached. Switch to a branch first.";

    public string CommitCode()
    {
        if (RepositoryProblem() is string problem)
        {
            return problem;
        }

        if (CurrentBranch() is not string branch)
        {
            return DetachedHead;
        }

        if (!HasChanges())
        {
            return $"Nothing to commit. {PushIfNeeded(branch)}";
        }

        CommitResult commit = CommitChanges(branch);

        return commit.Committed
            ? $"{commit.Notice} {Push(branch)}"
            : commit.Notice;
    }

    public string? SwitchAndPull()
    {
        if (RepositoryProblem() is string problem)
        {
            return problem;
        }

        string? current = CurrentBranch();
        string? commitNotice = null;

        if (HasChanges())
        {
            string choice = console.Select(
                $"You have uncommitted changes.{Environment.NewLine}{Environment.NewLine}Commit them before switching?",
                [CommitFirst, Cancel]);

            if (choice != CommitFirst)
            {
                return "Switch and Pull cancelled. You have uncommitted changes.";
            }

            if (current is null)
            {
                return DetachedHead;
            }

            CommitResult commit = CommitChanges(current);

            if (!commit.Committed)
            {
                return commit.Notice;
            }

            commitNotice = $"{commit.Notice} {Push(current)}";
        }

        string? target = SelectBranch("Switch to which branch?", current);

        if (target is null)
        {
            return commitNotice;
        }

        TerminalProcessResult switched = Git("switch", target);

        if (switched.ExitCode != 0)
        {
            return Join(commitNotice, Failure($"Switch to {target}", switched));
        }

        if (!HasUpstream())
        {
            return Join(commitNotice, $"Switched to {target}. It has no upstream on origin, so there was nothing to pull.");
        }

        TerminalProcessResult pulled = Git("pull", "--no-edit");

        return Join(
            commitNotice,
            pulled.ExitCode == 0
                ? $"Switched to {target} and pulled the latest commits from origin."
                : $"Switched to {target}. {Failure("Pull", pulled)}");
    }

    public string? CreateBranch(DevCraftFeature feature)
    {
        if (RepositoryProblem() is string problem)
        {
            return problem;
        }

        string source = CurrentBranch() ?? "HEAD";
        string choice = console.Select($"Create a branch from {source}", [UseTemplate, CustomName, Back]);
        string? proposed;

        if (choice == Back)
        {
            return null;
        }

        if (choice == UseTemplate)
        {
            (proposed, string? notice) = ResolveTemplate(feature);

            if (proposed is null)
            {
                return notice;
            }
        }
        else
        {
            proposed = string.Empty;
        }

        ValueEditResult edit = console.EditValue("Create Branch", "Branch name", proposed, required: true);
        string name = edit.Value.Trim();

        if (!edit.Saved || name.Length == 0)
        {
            return NotCreated;
        }

        if (Git("check-ref-format", "--branch", name).ExitCode != 0)
        {
            return $"{name} is not a valid branch name. {NotCreated}";
        }

        if (Git("show-ref", "--verify", "--quiet", $"refs/heads/{name}").ExitCode == 0)
        {
            return $"Branch {name} already exists. {NotCreated}";
        }

        TerminalProcessResult created = Git("switch", "-c", name);

        return created.ExitCode == 0
            ? $"Created branch {name} from {source} and switched to it."
            : $"{Failure($"Create branch {name}", created)} {NotCreated}";
    }

    public string? MergeFrom()
    {
        if (RepositoryProblem() is string problem)
        {
            return problem;
        }

        if (CurrentBranch() is not string original)
        {
            return DetachedHead;
        }

        string? source = SelectBranch($"Merge which branch into {original}?", original);

        if (source is null)
        {
            return null;
        }

        string? commitNotice = null;

        if (HasChanges())
        {
            CommitResult commit = CommitChanges(original);

            if (!commit.Committed)
            {
                return commit.Notice;
            }

            commitNotice = commit.Notice;
        }

        TerminalProcessResult switched = Git("switch", source);

        if (switched.ExitCode != 0)
        {
            return Join(commitNotice, $"{Failure($"Switch to {source}", switched)} Nothing was merged.");
        }

        if (HasUpstream())
        {
            TerminalProcessResult pulled = Git("pull", "--no-edit");

            if (pulled.ExitCode != 0)
            {
                Git("switch", original);
                return Join(commitNotice, $"{Failure($"Pull of {source}", pulled)} Returned to {original}; nothing was merged.");
            }
        }

        TerminalProcessResult returned = Git("switch", original);

        if (returned.ExitCode != 0)
        {
            return Join(commitNotice, $"{Failure($"Switch back to {original}", returned)} Nothing was merged.");
        }

        TerminalProcessResult merged = Git("merge", "--no-edit", source);

        if (merged.ExitCode == 0)
        {
            return Join(commitNotice, $"Merged {source} into {original}.");
        }

        List<string> conflicts = Lines(Git("diff", "--name-only", "--diff-filter=U").StandardOutput);

        return Join(
            commitNotice,
            conflicts.Count > 0
                ? $"Merging {source} into {original} stopped with conflicts in: {string.Join(", ", conflicts)}. Resolve them and commit."
                : Failure($"Merge of {source} into {original}", merged));
    }

    private (string? BranchName, string? Notice) ResolveTemplate(DevCraftFeature feature)
    {
        DevCraftProfileConfiguration profile = ProfileConfigurationReader.Read(context.ProfileDirectory);
        GitToolingSettings tooling = profile.GitTooling;

        if (string.IsNullOrWhiteSpace(tooling.BranchTemplate))
        {
            return (null, "No branch template is set. Set one in Configure DevCraft > Configure GIT Tooling > Setup Branch Template.");
        }

        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        List<FieldEditField> fields = [];

        foreach (string token in BranchNameResolver.Tokens(tooling.BranchTemplate))
        {
            if (token.Equals(BranchNameResolver.FeatureTypeToken, StringComparison.OrdinalIgnoreCase))
            {
                values[token] = console.Select("Feature type", profile.FeatureTypes);
            }
            else if (token.Equals(BranchNameResolver.WorkItemToken, StringComparison.OrdinalIgnoreCase))
            {
                fields.Add(new FieldEditField(token, "Work item", feature.ExternalReference ?? string.Empty, Required: true));
            }
            else if (token.Equals(BranchNameResolver.TitleToken, StringComparison.OrdinalIgnoreCase))
            {
                fields.Add(new FieldEditField(token, "Title", feature.Name, Required: true));
            }
            else if (VariableValue(tooling, token) is string value && !IsInput(value))
            {
                values[token] = value;
            }
            else
            {
                fields.Add(new FieldEditField(token, token, string.Empty, Required: true));
            }
        }

        if (fields.Count > 0)
        {
            FieldEditResult edit = console.EditFields(new FieldEditOptions("Create Branch", fields));

            if (!edit.Saved)
            {
                return (null, NotCreated);
            }

            foreach (FieldEditField field in fields)
            {
                string value = edit.Values.TryGetValue(field.Key, out string? entered) ? entered.Trim() : string.Empty;

                if (value.Length == 0)
                {
                    return (null, $"{field.Label} is required. {NotCreated}");
                }

                values[field.Key] = value;
            }
        }

        BranchNameResolution resolution = BranchNameResolver.Resolve(tooling.BranchTemplate, values);

        return resolution.UnresolvedTokens.Count == 0
            ? (resolution.BranchName, null)
            : (null, $"No value for {string.Join(", ", resolution.UnresolvedTokens)}. {NotCreated}");
    }

    private static string? VariableValue(GitToolingSettings tooling, string token)
    {
        return tooling.BranchVariables.FirstOrDefault(pair => pair.Key.Equals(token, StringComparison.OrdinalIgnoreCase)).Value;
    }

    private static bool IsInput(string value)
    {
        return value.Trim().Equals(BranchNameResolver.InputValue, StringComparison.OrdinalIgnoreCase);
    }

    private CommitResult CommitChanges(string branch)
    {
        TerminalProcessResult staged = Git("add", "-A");

        if (staged.ExitCode != 0)
        {
            return new CommitResult(false, Failure("Staging changes", staged));
        }

        string stat = Git("diff", "--cached", "--stat").StandardOutput;

        if (stat.Trim().Length == 0)
        {
            return new CommitResult(false, "Nothing to commit.");
        }

        string diff = Git("diff", "--cached").StandardOutput;
        string prompt = CommitMessagePromptBuilder.Build(context.CurrentDirectory, branch, stat, diff);
        CommitMessageResponse response;

        try
        {
            SupportedTerminalClient client = DefaultTerminalClient.Resolve(context);
            string output = console.RunStatus(
                $"Writing a commit message with {client.Name}...",
                () => agent.Run(context, client, prompt));
            response = CommitMessageResponse.Parse(output);
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            response = new CommitMessageResponse(false, exception.Message, string.Empty);
        }

        if (!response.Success)
        {
            return new CommitResult(false, $"Commit Code failed: {response.Summary} The changes are staged and nothing was committed.");
        }

        TerminalProcessResult committed = Git("commit", "-m", response.Message);

        if (committed.ExitCode != 0)
        {
            return new CommitResult(false, $"{Failure("Commit", committed)} The changes are staged.");
        }

        string subject = response.Message.ReplaceLineEndings("\n").Split('\n')[0].Trim();
        return new CommitResult(true, $"Committed to {branch}: {subject}.");
    }

    private string PushIfNeeded(string branch)
    {
        if (!HasUpstream())
        {
            return Push(branch);
        }

        string ahead = Git("rev-list", "--count", "@{u}..HEAD").StandardOutput.Trim();

        return int.TryParse(ahead, out int count) && count > 0
            ? Push(branch)
            : $"{branch} is up to date with origin.";
    }

    private string Push(string branch)
    {
        if (Git("remote", "get-url", "origin").ExitCode != 0)
        {
            return "No origin remote is configured, so nothing was pushed.";
        }

        if (HasUpstream())
        {
            TerminalProcessResult pushed = Git("push");
            return pushed.ExitCode == 0 ? $"Pushed {branch} to origin." : Failure("Push", pushed);
        }

        TerminalProcessResult created = Git("push", "-u", "origin", branch);

        return created.ExitCode == 0
            ? $"Pushed {branch} to origin and set it as the upstream."
            : Failure("Push", created);
    }

    private string? SelectBranch(string title, string? exclude)
    {
        List<string> branches = [];

        foreach (string reference in Lines(Git("for-each-ref", "--format=%(refname:short)", "refs/heads", "refs/remotes/origin").StandardOutput))
        {
            string name = reference;

            if (reference == "origin" || reference == "origin/HEAD")
            {
                continue;
            }

            if (reference.StartsWith("origin/", StringComparison.Ordinal))
            {
                name = reference["origin/".Length..];
            }

            if (name != exclude && !branches.Contains(name, StringComparer.Ordinal))
            {
                branches.Add(name);
            }
        }

        if (branches.Count == 0)
        {
            return null;
        }

        string selected = console.Select(title, [.. branches, Back]);

        return selected == Back ? null : selected;
    }

    private string? RepositoryProblem()
    {
        return Git("rev-parse", "--is-inside-work-tree").ExitCode == 0
            ? null
            : "This folder is not a git repository.";
    }

    private string? CurrentBranch()
    {
        // symbolic-ref also names an unborn branch in a repository with no commits; it fails on a detached HEAD.
        TerminalProcessResult result = Git("symbolic-ref", "--short", "-q", "HEAD");
        string branch = result.StandardOutput.Trim();

        return result.ExitCode != 0 || branch.Length == 0 ? null : branch;
    }

    private bool HasChanges()
    {
        return Git("status", "--porcelain").StandardOutput.Trim().Length > 0;
    }

    private bool HasUpstream()
    {
        return Git("rev-parse", "--abbrev-ref", "--symbolic-full-name", "@{u}").ExitCode == 0;
    }

    private TerminalProcessResult Git(params string[] arguments)
    {
        return git.Run(context.CurrentDirectory, arguments);
    }

    private static string Failure(string step, TerminalProcessResult result)
    {
        string detail = result.StandardError.Trim().Length > 0 ? result.StandardError.Trim() : result.StandardOutput.Trim();

        return detail.Length == 0
            ? $"{step} failed (git exit code {result.ExitCode})."
            : $"{step} failed: {detail}";
    }

    private static string Join(string? first, string second)
    {
        return first is null ? second : $"{first} {second}";
    }

    private static List<string> Lines(string output)
    {
        return output
            .ReplaceLineEndings("\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private sealed record CommitResult(bool Committed, string Notice);
}
