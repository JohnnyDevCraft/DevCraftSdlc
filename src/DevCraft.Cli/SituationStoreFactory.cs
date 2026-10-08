namespace DevCraft.Cli;

public static class SituationStoreFactory
{
    public static ISituationStore Create(string profileDirectory, DevCraftProfileConfiguration configuration)
    {
        if (configuration.SituationStorage == SituationStorage.Database)
        {
            if (string.IsNullOrWhiteSpace(configuration.SituationConnection))
            {
                throw new InvalidOperationException("Situational awareness database storage needs a MongoDB connection string.");
            }

            return new MongoSituationStore(configuration.SituationConnection);
        }

        return new FileSituationStore(profileDirectory);
    }
}
