namespace DevCraft.Cli;

public static class SituationMigrationService
{
    public static void Migrate(
        string profileDirectory,
        DevCraftProfileConfiguration oldConfiguration,
        DevCraftProfileConfiguration newConfiguration)
    {
        if (!newConfiguration.SituationEnabled ||
            oldConfiguration.SituationStorage == newConfiguration.SituationStorage)
        {
            return;
        }

        ISituationStore source = SituationStoreFactory.Create(profileDirectory, oldConfiguration);
        bool uncompressedOnly = oldConfiguration.SituationStorage == SituationStorage.Database &&
            newConfiguration.SituationStorage == SituationStorage.File;
        SituationSnapshot snapshot = source.Read(uncompressedOnly);

        if (snapshot.People.Count == 0 &&
            snapshot.LogEntries.Count == 0 &&
            snapshot.Summaries.Count == 0)
        {
            return;
        }

        ISituationStore target = SituationStoreFactory.Create(profileDirectory, newConfiguration);

        target.Replace(snapshot);
    }
}
