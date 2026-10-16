namespace DevCraft.Cli;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

public static class ProjectDevCraftConfigurationStore
{
    // An empty Features list is left out so System Central repositories commit no feature index.
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { OmitEmptyFeatures },
        },
    };

    public static string PathFor(string projectDirectory)
    {
        return Path.Combine(projectDirectory, ".devcraft", "configure.json");
    }

    public static void Write(string projectDirectory, ProjectDevCraftConfiguration configuration)
    {
        string path = PathFor(projectDirectory);
        ProfileFolderGuard.ThrowIfProfile(Path.GetDirectoryName(path)!);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        File.WriteAllText(path, Serialize(configuration));
    }

    public static string Serialize(ProjectDevCraftConfiguration configuration)
    {
        return JsonSerializer.Serialize(configuration, Options);
    }

    private static void OmitEmptyFeatures(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type != typeof(ProjectDevCraftConfiguration))
        {
            return;
        }

        foreach (JsonPropertyInfo property in typeInfo.Properties)
        {
            if (property.Name == nameof(ProjectDevCraftConfiguration.Features))
            {
                property.ShouldSerialize = (_, value) => value is IReadOnlyList<DevCraftFeature> { Count: > 0 };
            }
        }
    }
}
