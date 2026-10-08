namespace DevCraft.Cli;

public static class CatalogMergeCommand
{
    public static void Run(StartupContext context, IReadOnlyList<string> args)
    {
        if (args.Count == 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            Console.WriteLine("Use merge <file>.");
            return;
        }

        string sourcePath = Path.GetFullPath(args[0]);
        if (!File.Exists(sourcePath))
        {
            Console.WriteLine($"Catalog merge file not found: {sourcePath}");
            return;
        }

        ProfileStructureInitializer.Ensure(context.ProfileDirectory);

        DevCraftProfileConfiguration target = ProfileConfigurationReader.Read(context.ProfileDirectory);
        DevCraftProfileConfiguration source = ProfileConfigurationReader.ReadFile(sourcePath);
        (DevCraftProfileConfiguration configuration, CatalogMergeResult result) = Merge(target, source);

        DevCraftProfileConfigurationWriter.Write(context.ProfileDirectory, configuration);

        Console.WriteLine($"Catalog merge complete: {result.Added} added, {result.Updated} updated.");
        Console.WriteLine($"  Skills: {result.SkillsAdded} added, {result.SkillsUpdated} updated");
        Console.WriteLine($"  Standards: {result.StandardsAdded} added, {result.StandardsUpdated} updated");
        Console.WriteLine($"  Architectures: {result.ArchitecturesAdded} added, {result.ArchitecturesUpdated} updated");
        Console.WriteLine($"  Project Types: {result.ProjectTypesAdded} added, {result.ProjectTypesUpdated} updated");
    }

    public static (DevCraftProfileConfiguration Configuration, CatalogMergeResult Result) Merge(
        DevCraftProfileConfiguration target,
        DevCraftProfileConfiguration source)
    {
        (IReadOnlyList<ProfileCatalogDocument> skills, int skillsAdded, int skillsUpdated) = MergeDocuments(target.Skills, source.Skills);
        (IReadOnlyList<ProfileCatalogDocument> standards, int standardsAdded, int standardsUpdated) = MergeDocuments(target.Standards, source.Standards);
        (IReadOnlyList<ProfileCatalogDocument> architectures, int architecturesAdded, int architecturesUpdated) = MergeDocuments(target.Architectures, source.Architectures);
        (IReadOnlyList<ProfileCatalogDocument> projectTypes, int projectTypesAdded, int projectTypesUpdated) = MergeDocuments(target.ProjectTypes, source.ProjectTypes);

        DevCraftProfileConfiguration configuration = new(
            skills,
            standards,
            architectures,
            target.Templates,
            projectTypes,
            target.FeatureStorageTypes,
            target.SelectedFeatureStorage,
            target.SupportedClients);

        return (
            configuration,
            new CatalogMergeResult(
                skillsAdded,
                skillsUpdated,
                standardsAdded,
                standardsUpdated,
                architecturesAdded,
                architecturesUpdated,
                projectTypesAdded,
                projectTypesUpdated));
    }

    private static (IReadOnlyList<ProfileCatalogDocument> Documents, int Added, int Updated) MergeDocuments(
        IReadOnlyList<ProfileCatalogDocument>? target,
        IReadOnlyList<ProfileCatalogDocument>? source)
    {
        List<ProfileCatalogDocument> documents = target is null
            ? []
            : [.. target];
        Dictionary<string, int> indexesBySlug = new(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < documents.Count; index++)
        {
            indexesBySlug[documents[index].Slug] = index;
        }

        int added = 0;
        int updated = 0;

        foreach (ProfileCatalogDocument sourceDocument in source ?? [])
        {
            if (indexesBySlug.TryGetValue(sourceDocument.Slug, out int existingIndex))
            {
                documents[existingIndex] = sourceDocument;
                updated++;
                continue;
            }

            indexesBySlug[sourceDocument.Slug] = documents.Count;
            documents.Add(sourceDocument);
            added++;
        }

        return (documents, added, updated);
    }
}
