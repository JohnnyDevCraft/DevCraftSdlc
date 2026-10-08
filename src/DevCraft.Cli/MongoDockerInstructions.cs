namespace DevCraft.Cli;

public static class MongoDockerInstructions
{
    public const string Text =
        """
        MongoDB Docker setup you can run in another terminal:

        docker run --name devcraft-mongo -p 27017:27017 -d mongo:7

        Connection string:
        mongodb://localhost:27017/DevCraft

        If the container already exists, start it with:
        docker start devcraft-mongo
        """;
}
