namespace DevCraft.Cli;

public static class SystemCentralProjectsSynchronizer
{
    // The central projects.json owns feature status; every other field flows from the repository.
    public static ProjectDevCraftConfiguration Sync(StartupContext context, ProjectDevCraftConfiguration projectConfiguration)
    {
        if (string.IsNullOrWhiteSpace(projectConfiguration.ProjectKey))
        {
            return projectConfiguration;
        }

        SystemCentralProjectsIndex index = SystemCentralProjectsStore.Read(context.ProfileDirectory);
        List<SystemCentralProject> projects = index.Projects.ToList();
        int projectIndex = projects.FindIndex(project => project.ProjectKey.Equals(projectConfiguration.ProjectKey, StringComparison.OrdinalIgnoreCase));
        SystemCentralProject project = projectIndex >= 0
            ? projects[projectIndex]
            : new SystemCentralProject(
                projectConfiguration.ProjectKey,
                projectConfiguration.ProjectProfile.Name,
                SlugGenerator.Create(projectConfiguration.ProjectProfile.Name),
                context.CurrentDirectory,
                []);

        List<DevCraftFeature> repositoryFeatures = projectConfiguration.Features
            .Select(feature => WithCentralStatus(project.Features, feature))
            .ToList();

        project = project with
        {
            RepositoryPath = context.CurrentDirectory,
            Features = MergeFeatures(project.Features, repositoryFeatures),
        };

        if (projectIndex >= 0)
        {
            projects[projectIndex] = project;
        }
        else
        {
            projects.Add(project);
        }

        SystemCentralProjectsStore.WriteIfChanged(context.ProfileDirectory, new SystemCentralProjectsIndex(projects));

        if (repositoryFeatures.SequenceEqual(projectConfiguration.Features))
        {
            return projectConfiguration;
        }

        ProjectDevCraftConfiguration updated = projectConfiguration with { Features = repositoryFeatures };
        ProjectDevCraftConfigurationStore.Write(context.CurrentDirectory, updated);

        return updated;
    }

    private static DevCraftFeature WithCentralStatus(IReadOnlyList<DevCraftFeature> centralFeatures, DevCraftFeature repositoryFeature)
    {
        int featureIndex = FindFeature(centralFeatures.ToList(), repositoryFeature);

        return featureIndex < 0 || string.IsNullOrWhiteSpace(centralFeatures[featureIndex].Status)
            ? repositoryFeature
            : repositoryFeature with { Status = centralFeatures[featureIndex].Status };
    }

    private static List<DevCraftFeature> MergeFeatures(
        IReadOnlyList<DevCraftFeature> centralFeatures,
        IReadOnlyList<DevCraftFeature> repositoryFeatures)
    {
        List<DevCraftFeature> features = centralFeatures.ToList();

        foreach (DevCraftFeature repositoryFeature in repositoryFeatures)
        {
            int featureIndex = FindFeature(features, repositoryFeature);

            if (featureIndex >= 0)
            {
                features[featureIndex] = repositoryFeature;
            }
            else
            {
                features.Add(repositoryFeature);
            }
        }

        return features;
    }

    private static int FindFeature(List<DevCraftFeature> features, DevCraftFeature repositoryFeature)
    {
        int featureIndex = string.IsNullOrWhiteSpace(repositoryFeature.FolderName)
            ? -1
            : features.FindIndex(feature => feature.FolderName.Equals(repositoryFeature.FolderName, StringComparison.OrdinalIgnoreCase));

        return featureIndex >= 0
            ? featureIndex
            : features.FindIndex(feature => feature.Slug.Equals(repositoryFeature.Slug, StringComparison.OrdinalIgnoreCase));
    }
}
