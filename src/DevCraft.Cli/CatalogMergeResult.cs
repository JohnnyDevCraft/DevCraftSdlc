namespace DevCraft.Cli;

public sealed record CatalogMergeResult(
    int SkillsAdded,
    int SkillsUpdated,
    int StandardsAdded,
    int StandardsUpdated,
    int ArchitecturesAdded,
    int ArchitecturesUpdated,
    int ProjectTypesAdded,
    int ProjectTypesUpdated)
{
    public int Added => SkillsAdded + StandardsAdded + ArchitecturesAdded + ProjectTypesAdded;

    public int Updated => SkillsUpdated + StandardsUpdated + ArchitecturesUpdated + ProjectTypesUpdated;
}
