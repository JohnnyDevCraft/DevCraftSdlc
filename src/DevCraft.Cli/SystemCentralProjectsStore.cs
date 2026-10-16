namespace DevCraft.Cli;

using System.Globalization;
using System.Text;
using System.Text.Json;

public static class SystemCentralProjectsStore
{
    public static string PathFor(string profileDirectory)
    {
        return Path.Combine(profileDirectory, "features", "projects.json");
    }

    public static SystemCentralProjectsIndex Read(string profileDirectory)
    {
        string path = PathFor(profileDirectory);

        if (!File.Exists(path))
        {
            return new SystemCentralProjectsIndex([]);
        }

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object ||
            !TryGetProperty(root, out JsonElement projectsElement, "Projects") ||
            projectsElement.ValueKind != JsonValueKind.Array)
        {
            return new SystemCentralProjectsIndex([]);
        }

        List<SystemCentralProject> projects = [];

        foreach (JsonElement projectElement in projectsElement.EnumerateArray())
        {
            if (projectElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            projects.Add(ReadProject(projectElement));
        }

        return new SystemCentralProjectsIndex(projects);
    }

    public static void Write(string profileDirectory, SystemCentralProjectsIndex index)
    {
        WriteAtomically(PathFor(profileDirectory), WriteJson(index));
    }

    // Reads, changes, and writes the index while holding the projects.json lock, so concurrent sessions
    // never overwrite each other's changes. Throws ProjectsIndexBusyException when the lock is not free in time.
    public static SystemCentralProjectsIndex Update(
        string profileDirectory,
        Func<SystemCentralProjectsIndex, SystemCentralProjectsIndex> change,
        TimeSpan? timeout = null)
    {
        using IDisposable held = ProjectsIndexLock.Acquire(profileDirectory, timeout);
        SystemCentralProjectsIndex updated = change(Read(profileDirectory));
        WriteIfChanged(profileDirectory, updated);

        return updated;
    }

    private static void WriteIfChanged(string profileDirectory, SystemCentralProjectsIndex index)
    {
        string path = PathFor(profileDirectory);
        string json = WriteJson(index);

        if (File.Exists(path) && File.ReadAllText(path) == json)
        {
            return;
        }

        WriteAtomically(path, json);
    }

    // A crash mid-write leaves only the temporary file behind; projects.json is replaced in one step.
    private static void WriteAtomically(string path, string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, path, overwrite: true);
    }

    private static string WriteJson(SystemCentralProjectsIndex index)
    {
        using MemoryStream stream = new();
        using (Utf8JsonWriter writer = new(
            stream,
            new JsonWriterOptions
            {
                Indented = true,
            }))
        {
            writer.WriteStartObject();
            writer.WriteStartArray("Projects");

            foreach (SystemCentralProject project in index.Projects)
            {
                writer.WriteStartObject();
                writer.WriteString("id", project.ProjectKey);
                writer.WriteString("name", project.Name);
                writer.WriteString("slug", project.Slug);
                writer.WriteString("repo-name", project.RepositoryName);
                WriteNullableString(writer, "repo-remote", project.RepositoryRemote);
                writer.WriteStartArray("working-copies");

                foreach (WorkingCopy copy in project.WorkingCopies)
                {
                    writer.WriteStartObject();
                    writer.WriteString("path", copy.Path);
                    WriteNullableString(writer, "branch", copy.Branch);
                    WriteNullableString(writer, "last-seen", copy.LastSeen?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteStartArray("features");

                foreach (DevCraftFeature feature in project.Features)
                {
                    writer.WriteStartObject();
                    writer.WriteString("id", feature.FolderName);
                    writer.WriteString("feature-name", feature.Name);
                    writer.WriteString("slug", feature.Slug);
                    writer.WriteString("description", feature.ShortDescription);
                    writer.WriteString("folder-name", feature.FolderName);
                    writer.WriteString("storage-type", feature.StorageType);
                    WriteNullableString(writer, "work-item-id", feature.WorkItemId);
                    WriteNullableString(writer, "external-reference", feature.ExternalReference);
                    WriteNullableString(writer, "branch", feature.Branch);
                    writer.WriteString("status", string.IsNullOrWhiteSpace(feature.Status) ? "Discovery" : feature.Status);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteNullableString(Utf8JsonWriter writer, string propertyName, string? value)
    {
        if (value is null)
        {
            writer.WriteNull(propertyName);
            return;
        }

        writer.WriteString(propertyName, value);
    }

    private static SystemCentralProject ReadProject(JsonElement projectElement)
    {
        string projectKey = ReadString(projectElement, "id", "ProjectKey");
        string name = ReadString(projectElement, "name", "Name");
        string slug = ReadString(projectElement, "slug", "Slug");
        List<DevCraftFeature> features = [];

        if (TryGetProperty(projectElement, out JsonElement featuresElement, "features", "Features") &&
            featuresElement.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement featureElement in featuresElement.EnumerateArray())
            {
                if (featureElement.ValueKind == JsonValueKind.Object)
                {
                    features.Add(ReadFeature(featureElement));
                }
            }
        }

        List<WorkingCopy> workingCopies = ReadWorkingCopies(projectElement);
        string repositoryName = ReadString(projectElement, "repo-name", "RepositoryName");

        if (repositoryName.Length == 0 && workingCopies.Count > 0)
        {
            repositoryName = SystemCentralProject.FolderName(workingCopies[0].Path);
        }

        return new SystemCentralProject(
            projectKey,
            name,
            slug,
            repositoryName,
            ReadNullableString(projectElement, "repo-remote", "RepositoryRemote"),
            workingCopies,
            features);
    }

    // Files written before working copies were tracked hold one repository path; it becomes the first copy.
    private static List<WorkingCopy> ReadWorkingCopies(JsonElement projectElement)
    {
        List<WorkingCopy> copies = [];

        if (TryGetProperty(projectElement, out JsonElement copiesElement, "working-copies", "WorkingCopies") &&
            copiesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement copyElement in copiesElement.EnumerateArray())
            {
                string path = copyElement.ValueKind == JsonValueKind.Object ? ReadString(copyElement, "path", "Path") : string.Empty;

                if (path.Length > 0)
                {
                    copies.Add(new WorkingCopy(
                        path,
                        ReadNullableString(copyElement, "branch", "Branch"),
                        ReadTimestamp(copyElement, "last-seen", "LastSeen")));
                }
            }

            return copies;
        }

        string legacyPath = ReadString(projectElement, "repo-location", "RepositoryPath");

        if (legacyPath.Length > 0)
        {
            copies.Add(new WorkingCopy(legacyPath, null, null));
        }

        return copies;
    }

    private static DateTimeOffset? ReadTimestamp(JsonElement element, params string[] names)
    {
        return DateTimeOffset.TryParse(
            ReadNullableString(element, names),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out DateTimeOffset value)
            ? value
            : null;
    }

    private static DevCraftFeature ReadFeature(JsonElement featureElement)
    {
        string name = ReadString(featureElement, "feature-name", "Name");
        string slug = ReadString(featureElement, "slug", "Slug");
        string description = ReadString(featureElement, "description", "ShortDescription");
        string folderName = ReadString(featureElement, "folder-name", "id", "FolderName");
        string storageType = ReadString(featureElement, "storage-type", "StorageType");
        string? externalReference = ReadNullableString(featureElement, "external-reference", "ExternalReference");
        string status = ReadString(featureElement, "status", "Status");

        return new DevCraftFeature(
            name,
            slug,
            description,
            folderName,
            storageType,
            externalReference,
            string.IsNullOrWhiteSpace(status) ? "Discovery" : status,
            ReadNullableString(featureElement, "work-item-id", "WorkItemId"),
            ReadNullableString(featureElement, "branch", "Branch"));
    }

    private static string ReadString(JsonElement element, params string[] names)
    {
        return ReadNullableString(element, names) ?? string.Empty;
    }

    private static string? ReadNullableString(JsonElement element, params string[] names)
    {
        return TryGetProperty(element, out JsonElement property, names) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static bool TryGetProperty(JsonElement element, out JsonElement property, params string[] names)
    {
        foreach (string name in names)
        {
            if (element.TryGetProperty(name, out property))
            {
                return true;
            }
        }

        property = default;
        return false;
    }
}
