namespace DevCraft.Cli;

public sealed record CatalogMergeResult(
    int SkillsAdded,
    int SkillsUpdated,
    int SkillsSkipped,
    int StandardsAdded,
    int StandardsUpdated,
    int StandardsSkipped,
    int ArchitecturesAdded,
    int ArchitecturesUpdated,
    int ArchitecturesSkipped,
    int ProjectTypesAdded,
    int ProjectTypesUpdated,
    int ProjectTypesSkipped)
{
    public int Added => SkillsAdded + StandardsAdded + ArchitecturesAdded + ProjectTypesAdded;

    public int Updated => SkillsUpdated + StandardsUpdated + ArchitecturesUpdated + ProjectTypesUpdated;

    public int Skipped => SkillsSkipped + StandardsSkipped + ArchitecturesSkipped + ProjectTypesSkipped;
}
