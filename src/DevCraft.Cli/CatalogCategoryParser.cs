namespace DevCraft.Cli;

public static class CatalogCategoryParser
{
    public static CatalogCategory Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return CatalogCategory.All;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "skill" or "skills" => CatalogCategory.Skills,
            "standard" or "standards" => CatalogCategory.Standards,
            "architecture" or "architectures" => CatalogCategory.Architectures,
            "template" or "templates" => CatalogCategory.Templates,
            "project-type" or "project-types" or "projects" => CatalogCategory.ProjectTypes,
            "feature-storage" or "feature-storage-types" or "storage" => CatalogCategory.FeatureStorageTypes,
            "client" or "clients" or "supported-client" or "supported-clients" => CatalogCategory.SupportedClients,
            _ => throw new ArgumentException($"Unknown list category: {value}"),
        };
    }
}
