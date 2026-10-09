namespace DevCraft.Cli;

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
        string path = PathFor(profileDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        string json = WriteJson(index);

        File.WriteAllText(path, json);
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
                writer.WriteString("repo-location", project.RepositoryPath);
                writer.WriteString("repo-name", project.RepositoryName);
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
                    WriteNullableString(writer, "work-item-id", FeatureWorkItemId(feature));
                    WriteNullableString(writer, "external-reference", feature.ExternalReference);
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
        string repositoryPath = ReadString(projectElement, "repo-location", "RepositoryPath");
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

        return new SystemCentralProject(projectKey, name, slug, repositoryPath, features);
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
            string.IsNullOrWhiteSpace(status) ? "Discovery" : status);
    }

    private static string? FeatureWorkItemId(DevCraftFeature feature)
    {
        if (FeatureStorageMode.IsLocal(feature.StorageType))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(feature.ExternalReference)
            ? null
            : feature.ExternalReference.Trim();
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
