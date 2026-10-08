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
        (DevCraftProfileConfiguration sourceWithExistingFiles, CatalogMergeResult skipped) = KeepExistingFiles(source, Path.GetDirectoryName(sourcePath) ?? Environment.CurrentDirectory);
        (DevCraftProfileConfiguration configuration, CatalogMergeResult result) = Merge(target, sourceWithExistingFiles, skipped);

        DevCraftProfileConfigurationWriter.Write(context.ProfileDirectory, configuration);

        Console.WriteLine($"Catalog merge complete: {result.Added} added, {result.Updated} updated, {result.Skipped} skipped.");
        Console.WriteLine($"  Skills: {result.SkillsAdded} added, {result.SkillsUpdated} updated, {result.SkillsSkipped} skipped");
        Console.WriteLine($"  Standards: {result.StandardsAdded} added, {result.StandardsUpdated} updated, {result.StandardsSkipped} skipped");
        Console.WriteLine($"  Architectures: {result.ArchitecturesAdded} added, {result.ArchitecturesUpdated} updated, {result.ArchitecturesSkipped} skipped");
        Console.WriteLine($"  Project Types: {result.ProjectTypesAdded} added, {result.ProjectTypesUpdated} updated, {result.ProjectTypesSkipped} skipped");
    }

    public static (DevCraftProfileConfiguration Configuration, CatalogMergeResult Result) Merge(
        DevCraftProfileConfiguration target,
        DevCraftProfileConfiguration source)
    {
        return Merge(
            target,
            source,
            new CatalogMergeResult(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
    }

    private static (DevCraftProfileConfiguration Configuration, CatalogMergeResult Result) Merge(
        DevCraftProfileConfiguration target,
        DevCraftProfileConfiguration source,
        CatalogMergeResult skipped)
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
                skipped.SkillsSkipped,
                standardsAdded,
                standardsUpdated,
                skipped.StandardsSkipped,
                architecturesAdded,
                architecturesUpdated,
                skipped.ArchitecturesSkipped,
                projectTypesAdded,
                projectTypesUpdated,
                skipped.ProjectTypesSkipped));
    }

    private static (DevCraftProfileConfiguration Configuration, CatalogMergeResult Skipped) KeepExistingFiles(
        DevCraftProfileConfiguration source,
        string catalogDirectory)
    {
        (IReadOnlyList<ProfileCatalogDocument> skills, int skillsSkipped) = KeepExistingDocuments(source.Skills, catalogDirectory);
        (IReadOnlyList<ProfileCatalogDocument> standards, int standardsSkipped) = KeepExistingDocuments(source.Standards, catalogDirectory);
        (IReadOnlyList<ProfileCatalogDocument> architectures, int architecturesSkipped) = KeepExistingDocuments(source.Architectures, catalogDirectory);
        (IReadOnlyList<ProfileCatalogDocument> projectTypes, int projectTypesSkipped) = KeepExistingDocuments(source.ProjectTypes, catalogDirectory);

        return (
            new DevCraftProfileConfiguration(
                skills,
                standards,
                architectures,
                source.Templates,
                projectTypes,
                source.FeatureStorageTypes,
                source.SelectedFeatureStorage,
                source.SupportedClients),
            new CatalogMergeResult(
                0,
                0,
                skillsSkipped,
                0,
                0,
                standardsSkipped,
                0,
                0,
                architecturesSkipped,
                0,
                0,
                projectTypesSkipped));
    }

    private static (IReadOnlyList<ProfileCatalogDocument> Documents, int Skipped) KeepExistingDocuments(
        IReadOnlyList<ProfileCatalogDocument>? documents,
        string catalogDirectory)
    {
        List<ProfileCatalogDocument> existing = [];
        int skipped = 0;

        foreach (ProfileCatalogDocument document in documents ?? [])
        {
            if (File.Exists(ResolveDocumentPath(catalogDirectory, document.Path)))
            {
                existing.Add(document);
                continue;
            }

            skipped++;
        }

        return (existing, skipped);
    }

    private static string ResolveDocumentPath(string catalogDirectory, string documentPath)
    {
        return Path.IsPathRooted(documentPath)
            ? documentPath
            : Path.GetFullPath(Path.Combine(catalogDirectory, documentPath));
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
