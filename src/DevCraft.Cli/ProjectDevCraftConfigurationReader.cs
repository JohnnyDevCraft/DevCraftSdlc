namespace DevCraft.Cli;

using System.Text.Json;

public static class ProjectDevCraftConfigurationReader
{
    private const string Pending = "Pending.";

    public static ProjectDevCraftConfiguration? Read(string projectDirectory)
    {
        string path = Path.Combine(projectDirectory, ".devcraft", "configure.json");

        if (!File.Exists(path))
        {
            return null;
        }

        ProjectDevCraftConfiguration? configuration = JsonSerializer.Deserialize<ProjectDevCraftConfiguration>(
            File.ReadAllText(path),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

        if (configuration is null)
        {
            return null;
        }

        // Hand-written or agent-written files may leave out values the record declares non-nullable.
        ProjectProfile? profile = configuration.ProjectProfile;
        string folderName = Path.GetFileName(Path.TrimEndingDirectorySeparator(Path.GetFullPath(projectDirectory)));

        return configuration with
        {
            ProjectKey = configuration.ProjectKey ?? string.Empty,
            FeaturesIndexPath = configuration.FeaturesIndexPath ?? "features.json",
            ProjectProfile = new ProjectProfile(
                string.IsNullOrWhiteSpace(profile?.Name) ? folderName : profile.Name,
                profile?.Purpose ?? Pending,
                profile?.Description ?? Pending),
            Features = configuration.Features ?? [],
        };
    }
}

