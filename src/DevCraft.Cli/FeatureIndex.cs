namespace DevCraft.Cli;

// The project's feature list for the current working copy. projects.json indexes every feature of every
// storage type; the repository configure.json lists only repo-central features, so nothing personal is committed.
// Loading records this working copy, refreshes the repository name and remote, offers to adopt an existing
// project key when the remote matches, and moves any other feature entries out of configure.json.
public sealed class FeatureIndex
{
    private const string Yes = "Yes";
    private const string No = "No";
    private const string NoneOfThese = "None";

    private readonly StartupContext context;

    private FeatureIndex(
        StartupContext context,
        ProjectDevCraftConfiguration configuration,
        SystemCentralProject project,
        GitRepositoryInfo currentCopy,
        IReadOnlyList<string> notices)
    {
        this.context = context;
        Configuration = configuration;
        Project = project;
        CurrentCopy = currentCopy;
        Notices = notices;
    }

    public ProjectDevCraftConfiguration Configuration { get; }

    public SystemCentralProject Project { get; }

    public GitRepositoryInfo CurrentCopy { get; }

    public IReadOnlyList<string> Notices { get; }

    public IReadOnlyList<DevCraftFeature> Features => Project.Features;

    public string CurrentDirectory => context.CurrentDirectory;

    public static FeatureIndex Load(
        StartupContext context,
        ProjectDevCraftConfiguration configuration,
        IGitRunner git,
        IConsoleInteraction console,
        TimeProvider? clock = null)
    {
        GitRepositoryInfo currentCopy = GitRepositoryInfo.Read(git, context.CurrentDirectory);

        // Without a project key there is no central entry to match, so the repository list stands alone.
        if (string.IsNullOrWhiteSpace(configuration.ProjectKey))
        {
            return new FeatureIndex(context, configuration, Detached(configuration), currentCopy, []);
        }

        string? remote = RemoteUrl.Normalize(currentCopy.RemoteUrl);
        List<string> notices = [];

        configuration = AdoptMatchingProjectKey(context, configuration, remote, console, notices);

        string path = WorkingCopyPaths.Normalize(context.CurrentDirectory);
        WorkingCopy copy = new(path, currentCopy.Branch, (clock ?? TimeProvider.System).GetUtcNow());
        SystemCentralProject? loaded = null;

        SystemCentralProjectsStore.Update(context.ProfileDirectory, index =>
        {
            SystemCentralProject project = FindProject(index, configuration.ProjectKey)
                ?? new SystemCentralProject(
                    configuration.ProjectKey,
                    configuration.ProjectProfile.Name,
                    SlugGenerator.Create(configuration.ProjectProfile.Name),
                    string.Empty,
                    null,
                    [],
                    []);

            loaded = project with
            {
                RepositoryName = RemoteUrl.RepositoryName(remote) ?? SystemCentralProject.FolderName(path),
                RepositoryRemote = remote,
                WorkingCopies = WithCopy(project.WorkingCopies, copy),
                Features = MergeRepositoryFeatures(project.Features, configuration.Features),
            };

            return WithProject(index, loaded);
        });

        SystemCentralProject current = loaded!;
        ProjectDevCraftConfiguration updated = configuration with
        {
            Features = RepositoryFeatures(configuration.Features, current.Features),
        };
        int moved = configuration.Features.Count(feature => !IsRepoCentral(feature));

        if (!updated.Features.SequenceEqual(configuration.Features))
        {
            ProjectDevCraftConfigurationStore.Write(context.CurrentDirectory, updated);
        }

        if (moved > 0)
        {
            notices.Add(
                $"Moved {moved} feature {(moved == 1 ? "entry" : "entries")} from .devcraft/configure.json into the central projects index. Commit the updated configure.json.");
        }

        return new FeatureIndex(context, updated, current, currentCopy, notices);
    }

    public FeatureIndex Save(params DevCraftFeature[] features)
    {
        SystemCentralProject saved = Project with { Features = Upsert(Project.Features, features) };

        if (!string.IsNullOrWhiteSpace(Configuration.ProjectKey))
        {
            SystemCentralProjectsStore.Update(context.ProfileDirectory, index =>
            {
                SystemCentralProject project = FindProject(index, Configuration.ProjectKey) ?? Project;
                saved = project with { Features = Upsert(project.Features, features) };

                return WithProject(index, saved);
            });
        }

        List<DevCraftFeature> repository = Configuration.Features.ToList();

        foreach (DevCraftFeature feature in features)
        {
            int position = repository.FindIndex(item => SameFolder(item, feature));

            if (!IsRepoCentral(feature))
            {
                if (position >= 0)
                {
                    repository.RemoveAt(position);
                }
            }
            else if (position >= 0)
            {
                repository[position] = feature;
            }
            else
            {
                repository.Add(feature);
            }
        }

        ProjectDevCraftConfiguration configuration = Configuration with { Features = repository };

        if (!repository.SequenceEqual(Configuration.Features))
        {
            ProjectDevCraftConfigurationStore.Write(context.CurrentDirectory, configuration);
        }

        return new FeatureIndex(context, configuration, saved, CurrentCopy, []);
    }

    private static List<DevCraftFeature> Upsert(IReadOnlyList<DevCraftFeature> existing, IReadOnlyList<DevCraftFeature> features)
    {
        List<DevCraftFeature> list = existing.ToList();

        foreach (DevCraftFeature feature in features)
        {
            int position = list.FindIndex(item => SameFolder(item, feature));

            if (position >= 0)
            {
                list[position] = feature;
            }
            else
            {
                list.Add(feature);
            }
        }

        return list;
    }

    private static SystemCentralProject Detached(ProjectDevCraftConfiguration configuration)
    {
        return new SystemCentralProject(
            string.Empty,
            configuration.ProjectProfile.Name,
            SlugGenerator.Create(configuration.ProjectProfile.Name),
            string.Empty,
            null,
            [],
            configuration.Features);
    }

    private static ProjectDevCraftConfiguration AdoptMatchingProjectKey(
        StartupContext context,
        ProjectDevCraftConfiguration configuration,
        string? remote,
        IConsoleInteraction console,
        List<string> notices)
    {
        SystemCentralProjectsIndex index = SystemCentralProjectsStore.Read(context.ProfileDirectory);

        if (remote is null || FindProject(index, configuration.ProjectKey) is not null)
        {
            return configuration;
        }

        List<SystemCentralProject> matches = index.Projects
            .Where(project => string.Equals(project.RepositoryRemote, remote, StringComparison.Ordinal))
            .ToList();
        SystemCentralProject? adopted = matches.Count switch
        {
            0 => null,
            1 => console.Select($"This clone looks like {matches[0].Name}. Use its project key?", [No, Yes]) == Yes ? matches[0] : null,
            _ => PickProject(console, matches),
        };

        if (adopted is null)
        {
            return configuration;
        }

        ProjectDevCraftConfiguration rekeyed = configuration with { ProjectKey = adopted.ProjectKey };
        ProjectDevCraftConfigurationStore.Write(context.CurrentDirectory, rekeyed);
        notices.Add($"This clone now uses the project key of {adopted.Name}. Commit the updated .devcraft/configure.json.");

        return rekeyed;
    }

    private static SystemCentralProject? PickProject(IConsoleInteraction console, IReadOnlyList<SystemCentralProject> matches)
    {
        string selected = console.Select(
            "This clone's remote matches more than one project. Use the project key of:",
            [.. matches.Select(project => project.Name), NoneOfThese]);

        return selected == NoneOfThese ? null : matches.FirstOrDefault(project => project.Name == selected);
    }

    // Entries from configure.json are added when missing. A repo-central entry is owned by the repository
    // except for its status; any other entry yields to the central record.
    private static List<DevCraftFeature> MergeRepositoryFeatures(
        IReadOnlyList<DevCraftFeature> central,
        IReadOnlyList<DevCraftFeature> repository)
    {
        List<DevCraftFeature> features = central.ToList();

        foreach (DevCraftFeature feature in repository)
        {
            int position = FindFeature(features, feature);

            if (position < 0)
            {
                features.Add(feature);
            }
            else if (IsRepoCentral(feature))
            {
                features[position] = WithCentralStatus(feature, features[position]);
            }
        }

        return features;
    }

    private static List<DevCraftFeature> RepositoryFeatures(
        IReadOnlyList<DevCraftFeature> repository,
        IReadOnlyList<DevCraftFeature> central)
    {
        return repository
            .Where(IsRepoCentral)
            .Select(feature => FindFeature(central.ToList(), feature) is int position and >= 0
                ? WithCentralStatus(feature, central[position])
                : feature)
            .ToList();
    }

    private static DevCraftFeature WithCentralStatus(DevCraftFeature repository, DevCraftFeature central)
    {
        return string.IsNullOrWhiteSpace(central.Status) ? repository : repository with { Status = central.Status };
    }

    private static IReadOnlyList<WorkingCopy> WithCopy(IReadOnlyList<WorkingCopy> copies, WorkingCopy copy)
    {
        List<WorkingCopy> list = copies.ToList();
        int position = list.FindIndex(item => WorkingCopyPaths.Same(item.Path, copy.Path));

        if (position >= 0)
        {
            list[position] = copy;
        }
        else
        {
            list.Add(copy);
        }

        return list;
    }

    private static SystemCentralProject? FindProject(SystemCentralProjectsIndex index, string projectKey)
    {
        return index.Projects.FirstOrDefault(project => project.ProjectKey.Equals(projectKey, StringComparison.OrdinalIgnoreCase));
    }

    private static SystemCentralProjectsIndex WithProject(SystemCentralProjectsIndex index, SystemCentralProject project)
    {
        List<SystemCentralProject> projects = index.Projects.ToList();
        int position = projects.FindIndex(item => item.ProjectKey.Equals(project.ProjectKey, StringComparison.OrdinalIgnoreCase));

        if (position >= 0)
        {
            projects[position] = project;
        }
        else
        {
            projects.Add(project);
        }

        return new SystemCentralProjectsIndex(projects);
    }

    private static int FindFeature(List<DevCraftFeature> features, DevCraftFeature feature)
    {
        int position = string.IsNullOrWhiteSpace(feature.FolderName)
            ? -1
            : features.FindIndex(item => SameFolder(item, feature));

        return position >= 0
            ? position
            : features.FindIndex(item => item.Slug.Equals(feature.Slug, StringComparison.OrdinalIgnoreCase));
    }

    private static bool SameFolder(DevCraftFeature first, DevCraftFeature second)
    {
        return first.FolderName.Equals(second.FolderName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRepoCentral(DevCraftFeature feature)
    {
        return feature.StorageType.Equals(FeatureStorageMode.RepoCentral, StringComparison.OrdinalIgnoreCase);
    }
}
