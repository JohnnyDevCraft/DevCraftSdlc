using System.Diagnostics.CodeAnalysis;

namespace DevCraft.Cli;

public static class FeatureCommand
{
    private const string Back = "Back";
    private const string Completed = "Completed";
    private const string CompleteStatus = "Complete";
    private const string SelectFeature = "Select a feature";
    private const string CompletedFeatures = "Completed features";
    private const string FeatureActions = "Feature actions";
    private const string EditFeatureName = "Edit Feature Name";
    private const string EditWorkItem = "Edit Work Item";
    private const string SetCurrentBranch = "Set Feature to Current Branch";
    private const string ClearBranch = "Clear Feature Branch";
    private const string Yes = "Yes";
    private const string No = "No";
    private const string BranchUnchanged = "Branch link is unchanged.";
    private const string OpenFeatureDirectory = "Open Feature Directory using Code";
    private const string StartConversation = "Start a conversation about the feature";
    private const string CommitCode = "Commit Code";
    private const string SwitchAndPull = "Switch and Pull";
    private const string CreateBranch = "Create Branch";
    private const string MergeFrom = "Merge From";

    public static void Run(
        StartupContext context,
        IReadOnlyList<string> args,
        IConsoleInteraction console,
        IFeatureAiSessionLauncher launcher,
        AgentHandoff? handoff = null,
        Func<AgentHandoff>? selectHandoff = null,
        IFeatureDirectoryOpener? directoryOpener = null,
        IGitRunner? gitRunner = null,
        IRepositoryDocumentAgent? commitAgent = null,
        string? workItemId = null,
        string? workItemUrl = null)
    {
        ProfileStructureInitializer.Ensure(context.ProfileDirectory);
        ProjectDevCraftConfiguration? projectConfiguration = ProjectDevCraftConfigurationReader.Read(context.CurrentDirectory);

        if (projectConfiguration is null)
        {
            console.WriteStatus("DevCraft is not initialized in this repository.");
            return;
        }

        IGitRunner git = gitRunner ?? new ProcessGitRunner();
        FeatureIndex index;

        try
        {
            index = FeatureIndex.Load(context, projectConfiguration, git, console);
        }
        catch (ProjectsIndexBusyException exception)
        {
            console.WriteStatus(exception.Message);
            return;
        }

        foreach (string notice in index.Notices)
        {
            console.WriteStatus(notice);
        }

        if (args.Count == 0)
        {
            console.WriteStatus("Use feature list or feature new <name>.");
            return;
        }

        if (args[0].Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            DevCraftProfileConfiguration profileConfiguration = ProfileConfigurationReader.Read(context.ProfileDirectory);
            GitFeatureActions gitActions = new(
                context,
                console,
                git,
                commitAgent ?? new TerminalRepositoryDocumentAgent());
            ListFeatures(context, index, profileConfiguration, console, launcher, handoff, selectHandoff, directoryOpener ?? new CodeFeatureDirectoryOpener(), gitActions, git);
            return;
        }

        if (args[0].Equals("new", StringComparison.OrdinalIgnoreCase))
        {
            string featureName = string.Join(' ', args.Skip(1)).Trim();

            if (string.IsNullOrWhiteSpace(featureName))
            {
                console.WriteStatus("Feature name is required.");
                return;
            }

            if (!TryResolveHandoff(context, console, handoff, selectHandoff, out AgentHandoff? featureHandoff))
            {
                return;
            }

            DevCraftProfileConfiguration profileConfiguration = ProfileConfigurationReader.Read(context.ProfileDirectory);
            CreateFeature(context, index, profileConfiguration, featureName, workItemId, workItemUrl, console, launcher, featureHandoff);
            return;
        }

        console.WriteStatus("Use feature list or feature new <name>.");
    }

    private static void ListFeatures(
        StartupContext context,
        FeatureIndex index,
        DevCraftProfileConfiguration profileConfiguration,
        IConsoleInteraction console,
        IFeatureAiSessionLauncher launcher,
        AgentHandoff? handoff,
        Func<AgentHandoff>? selectHandoff,
        IFeatureDirectoryOpener directoryOpener,
        GitFeatureActions gitActions,
        IGitRunner git)
    {
        IReadOnlyList<DevCraftFeature> features = index.Features;

        if (features.Count == 0)
        {
            console.WriteStatus("No features found.");
            return;
        }

        DevCraftFeature? selectedFeature = SelectFeatureFromList(console, features, index);

        if (selectedFeature is null)
        {
            return;
        }

        DevCraftFeature feature = selectedFeature;
        console.WriteStatus($"Selected feature: {feature.Name}");

        string? notice = null;

        while (true)
        {
            string action = console.Select(
                notice is null
                    ? FeatureActions
                    : $"{notice}{Environment.NewLine}{Environment.NewLine}{FeatureActions}",
                FeatureActionChoices(feature));

            if (action == Back)
            {
                return;
            }

            if (action == EditFeatureName)
            {
                (index, feature, notice) = RenameFeature(index, feature, console);
                console.WriteStatus(notice);
                continue;
            }

            if (action == EditWorkItem)
            {
                (index, feature, notice) = EditFeatureWorkItem(index, feature, console);
                console.WriteStatus(notice);
                continue;
            }

            if (action == SetCurrentBranch)
            {
                (index, feature, notice) = LinkCurrentBranch(context, index, feature, console, git);
                console.WriteStatus(notice);
                continue;
            }

            if (action == ClearBranch)
            {
                (index, feature, notice) = ClearFeatureBranch(index, feature, console);
                console.WriteStatus(notice);
                continue;
            }

            if (action == OpenFeatureDirectory)
            {
                notice = OpenDirectory(context, feature, directoryOpener);
                console.WriteStatus(notice);
                continue;
            }

            if (action is CommitCode or SwitchAndPull or CreateBranch or MergeFrom)
            {
                notice = RunGitAction(action, feature, gitActions);
                continue;
            }

            if (!TryResolveHandoff(context, console, handoff, selectHandoff, out AgentHandoff? conversationHandoff))
            {
                return;
            }

            launcher.Launch(
                context,
                FeatureStorageResolver.WithEffectiveStorage(profileConfiguration, index.Configuration),
                feature,
                conversationHandoff,
                "Work on existing feature.");
            return;
        }
    }

    private static List<string> FeatureActionChoices(DevCraftFeature feature)
    {
        List<string> choices = [EditFeatureName, EditWorkItem, SetCurrentBranch];

        if (feature.Branch is not null)
        {
            choices.Add(ClearBranch);
        }

        choices.AddRange([OpenFeatureDirectory, StartConversation, CommitCode, SwitchAndPull, CreateBranch, MergeFrom, Back]);

        return choices;
    }

    // A branch belongs to at most one feature and a feature has at most one branch; any change is confirmed.
    private static (FeatureIndex Index, DevCraftFeature Feature, string Notice) LinkCurrentBranch(
        StartupContext context,
        FeatureIndex index,
        DevCraftFeature feature,
        IConsoleInteraction console,
        IGitRunner git)
    {
        GitRepositoryInfo copy = GitRepositoryInfo.Read(git, context.CurrentDirectory);

        if (!copy.IsRepository)
        {
            return (index, feature, "This folder is not a git repository.");
        }

        if (copy.Branch is not string branch)
        {
            return (index, feature, "HEAD is detached. Switch to a branch first.");
        }

        if (feature.Branch == branch)
        {
            return (index, feature, $"{feature.Name} is already linked to {branch}.");
        }

        if (feature.Branch is string current && !Confirm(console, $"Replace {current} with {branch}?"))
        {
            return (index, feature, BranchUnchanged);
        }

        DevCraftFeature? holder = index.Features.FirstOrDefault(
            item => item.Branch == branch && !item.FolderName.Equals(feature.FolderName, StringComparison.OrdinalIgnoreCase));

        if (holder is not null && !Confirm(console, $"Move {branch} from {holder.Name} to this feature?"))
        {
            return (index, feature, BranchUnchanged);
        }

        DevCraftFeature linked = feature with { Branch = branch };
        string notice = $"{feature.Name} is linked to {branch}.";

        return holder is null
            ? TrySave(index, feature, notice, linked)
            : TrySave(index, feature, notice, linked, holder with { Branch = null });
    }

    private static (FeatureIndex Index, DevCraftFeature Feature, string Notice) ClearFeatureBranch(
        FeatureIndex index,
        DevCraftFeature feature,
        IConsoleInteraction console)
    {
        if (feature.Branch is not string branch || !Confirm(console, $"Clear branch {branch} from this feature?"))
        {
            return (index, feature, BranchUnchanged);
        }

        return TrySave(index, feature, $"Branch {branch} cleared from {feature.Name}.", feature with { Branch = null });
    }

    private static bool Confirm(IConsoleInteraction console, string question)
    {
        return console.Select(question, [No, Yes]) == Yes;
    }

    private static string? RunGitAction(string action, DevCraftFeature feature, GitFeatureActions gitActions)
    {
        try
        {
            return action switch
            {
                CommitCode => gitActions.CommitCode(),
                SwitchAndPull => gitActions.SwitchAndPull(),
                CreateBranch => gitActions.CreateBranch(feature),
                _ => gitActions.MergeFrom(),
            };
        }
        catch (InvalidOperationException exception)
        {
            return exception.Message;
        }
    }

    private static (FeatureIndex Index, DevCraftFeature Feature, string Notice) RenameFeature(
        FeatureIndex index,
        DevCraftFeature feature,
        IConsoleInteraction console)
    {
        ValueEditResult edit = console.EditValue("Edit Feature Name", "Feature name", feature.Name, required: true);

        if (!edit.Saved)
        {
            return (index, feature, "Feature name is unchanged.");
        }

        string newName = edit.Value.Trim();

        if (string.IsNullOrWhiteSpace(newName))
        {
            return (index, feature, "Feature name is required.");
        }

        if (newName.Equals(feature.Name, StringComparison.Ordinal))
        {
            return (index, feature, "Feature name is unchanged.");
        }

        DevCraftFeature renamedFeature = feature with
        {
            Name = newName,
        };

        return TrySave(index, feature, $"Feature renamed: {feature.Name} -> {newName}", renamedFeature);
    }

    private static (FeatureIndex Index, DevCraftFeature Feature, string Notice) EditFeatureWorkItem(
        FeatureIndex index,
        DevCraftFeature feature,
        IConsoleInteraction console)
    {
        FieldEditResult edit = WorkItemForm.Edit(
            console,
            EditWorkItem,
            WorkItemForm.Fields(feature.WorkItemId, feature.ExternalReference));
        DevCraftFeature edited = feature with
        {
            WorkItemId = WorkItemForm.Value(edit, WorkItemForm.WorkItemIdKey),
            ExternalReference = WorkItemForm.Value(edit, WorkItemForm.WorkItemUrlKey),
        };

        return !edit.Saved || edited == feature
            ? (index, feature, "Work item is unchanged.")
            : TrySave(index, feature, $"Work item updated for {feature.Name}.", edited);
    }

    // Saves through the central index; a busy projects.json leaves the feature as it was.
    private static (FeatureIndex Index, DevCraftFeature Feature, string Notice) TrySave(
        FeatureIndex index,
        DevCraftFeature feature,
        string notice,
        params DevCraftFeature[] changed)
    {
        try
        {
            return (index.Save(changed), changed[0], notice);
        }
        catch (ProjectsIndexBusyException exception)
        {
            return (index, feature, exception.Message);
        }
    }

    private static string OpenDirectory(
        StartupContext context,
        DevCraftFeature feature,
        IFeatureDirectoryOpener directoryOpener)
    {
        string? directory = FeatureDirectory(context, feature);

        if (directory is null)
        {
            return $"{feature.Name} uses {feature.StorageType} storage and has no local feature directory.";
        }

        if (!Directory.Exists(directory))
        {
            return $"Feature directory not found: {directory}";
        }

        return directoryOpener.Open(directory)
            ? $"Opened feature directory in Visual Studio Code: {directory}"
            : "Could not start Visual Studio Code. Make sure the `code` command is installed and on your PATH.";
    }

    private static string? FeatureDirectory(StartupContext context, DevCraftFeature feature)
    {
        return FeatureDirectories.For(context, feature.StorageType, feature.FolderName);
    }

    private static void CreateFeature(
        StartupContext context,
        FeatureIndex index,
        DevCraftProfileConfiguration profileConfiguration,
        string featureName,
        string? workItemId,
        string? workItemUrl,
        IConsoleInteraction console,
        IFeatureAiSessionLauncher launcher,
        AgentHandoff handoff)
    {
        string storageType = FeatureStorageResolver.Resolve(profileConfiguration, index.Configuration);
        string slug = UniqueSlug(SlugGenerator.Create(featureName), index.Features);
        DevCraftFeature feature = new(
            featureName,
            slug,
            "Pending.",
            Guid.NewGuid().ToString(),
            storageType,
            Clean(workItemUrl),
            WorkItemId: Clean(workItemId));
        FeatureIndex updated;

        try
        {
            updated = index.Save(feature);
        }
        catch (ProjectsIndexBusyException exception)
        {
            console.WriteStatus(exception.Message);
            return;
        }

        if (FeatureDirectory(context, feature) is string featureDirectory)
        {
            Directory.CreateDirectory(featureDirectory);
        }

        console.WriteStatus($"Feature created: {feature.Name}");
        launcher.Launch(
            context,
            FeatureStorageResolver.WithEffectiveStorage(profileConfiguration, updated.Configuration),
            feature,
            handoff,
            "Create a new feature.");
    }

    // In-progress features are listed by default; Completed switches to finished ones and Back returns.
    // The feature linked to this working copy's branch comes first, marked; others show where they are checked out.
    private static DevCraftFeature? SelectFeatureFromList(IConsoleInteraction console, IReadOnlyList<DevCraftFeature> features, FeatureIndex index)
    {
        bool showCompleted = false;

        while (true)
        {
            List<(string Choice, DevCraftFeature Feature)> visible = showCompleted
                ? features.Where(IsComplete).Select(feature => (FeatureChoice(feature), feature)).ToList()
                : InProgressChoices(features, index);
            List<string> choices = showCompleted ? [Back] : [Back, Completed];
            choices.AddRange(visible.Select(item => item.Choice));
            string selected = console.Select(showCompleted ? CompletedFeatures : SelectFeature, choices);

            if (selected == Back)
            {
                if (!showCompleted)
                {
                    return null;
                }

                showCompleted = false;
                continue;
            }

            if (selected == Completed && !showCompleted)
            {
                showCompleted = true;
                continue;
            }

            return visible.First(item => item.Choice == selected).Feature;
        }
    }

    private static List<(string Choice, DevCraftFeature Feature)> InProgressChoices(IReadOnlyList<DevCraftFeature> features, FeatureIndex index)
    {
        string? branch = index.CurrentCopy.Branch;
        IReadOnlyList<WorkingCopy> copies = index.Project.WorkingCopies;
        IReadOnlyList<string> names = WorkingCopyNames.Shortest(copies.Select(copy => copy.Path).ToList(), index.Project.RepositoryName);
        string here = WorkingCopyPaths.Normalize(index.CurrentDirectory);

        string CheckedOutIn(DevCraftFeature feature)
        {
            List<string> elsewhere = copies
                .Select((copy, position) => (Copy: copy, Name: names[position]))
                .Where(item => feature.Branch is not null
                    && item.Copy.Branch == feature.Branch
                    && !WorkingCopyPaths.Same(item.Copy.Path, here))
                .Select(item => Directory.Exists(item.Copy.Path) ? item.Name : $"{item.Name} (missing)")
                .ToList();

            return elsewhere.Count == 0 ? string.Empty : $" → {string.Join(", ", elsewhere)}";
        }

        List<DevCraftFeature> inProgress = features.Where(feature => !IsComplete(feature)).ToList();
        DevCraftFeature? current = branch is null ? null : inProgress.FirstOrDefault(feature => feature.Branch == branch);
        List<(string Choice, DevCraftFeature Feature)> choices = [];

        if (current is not null)
        {
            choices.Add(($"● {FeatureChoice(current)} (current branch){CheckedOutIn(current)}", current));
        }

        choices.AddRange(inProgress
            .Where(feature => !ReferenceEquals(feature, current))
            .Select(feature => ($"{FeatureChoice(feature)}{CheckedOutIn(feature)}", feature)));

        return choices;
    }

    private static bool IsComplete(DevCraftFeature feature)
    {
        return feature.Status.Equals(CompleteStatus, StringComparison.OrdinalIgnoreCase);
    }

    private static string FeatureChoice(DevCraftFeature feature)
    {
        return $"{feature.Name} ({feature.Slug})";
    }

    private static bool TryResolveHandoff(
        StartupContext context,
        IConsoleInteraction console,
        AgentHandoff? handoff,
        Func<AgentHandoff>? selectHandoff,
        [NotNullWhen(true)] out AgentHandoff? resolved)
    {
        try
        {
            resolved = handoff
                ?? selectHandoff?.Invoke()
                ?? AgentHandoffPrompter.Resolve(console, () => SelectFallbackClient(context));
            return true;
        }
        catch (AgentHandoffCancelledException exception)
        {
            console.WriteStatus(exception.Message);
            resolved = null;
            return false;
        }
    }

    private static string? Clean(string? value)
    {
        string trimmed = value?.Trim() ?? string.Empty;

        return trimmed.Length == 0 ? null : trimmed;
    }

    private static string UniqueSlug(string slug, IReadOnlyList<DevCraftFeature> features)
    {
        string candidate = slug;
        int suffix = 2;

        while (features.Any(feature => feature.Slug.Equals(candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{slug}-{suffix}";
            suffix++;
        }

        return candidate;
    }

    private static SupportedTerminalClient SelectFallbackClient(StartupContext context)
    {
        IReadOnlyList<SupportedTerminalClient> clients = ProfileConfigurationReader.Read(context.ProfileDirectory).SupportedClients;

        return clients.Count > 0
            ? clients[0]
            : SupportedTerminalClientCatalog.Create()[0];
    }
}
