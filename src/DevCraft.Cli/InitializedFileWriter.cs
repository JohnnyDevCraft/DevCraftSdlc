namespace DevCraft.Cli;

public static class InitializedFileWriter
{
    public static void Ensure(string profileDirectory)
    {
        string path = Path.Combine(profileDirectory, "initialized.md");

        if (File.Exists(path))
        {
            return;
        }

        File.WriteAllText(path, Content());
    }

    private static string Content()
    {
        return """
            # DevCraft Initialized Context

            ## Purpose

            Use this file with `soul.md` and `configure.json` when starting an AI client from DevCraft.

            ## DevCraft Mode

            - Treat `soul.md` as the operator and assistant identity context.
            - Treat `configure.json` as the catalog of available skills, standards, architectures, templates, project types, and supported terminal clients.
            - Prefer referenced catalog entries by slug when asking DevCraft to use or export reusable guidance.
            - Keep DevCraft workflow state explicit and auditable.
            - Do not create, modify, or overwrite project workflow files unless DevCraft's current feature state allows implementation.
            - When a repository already contains `.devcraft`, operate in DevCraft mode rather than reinstalling DevCraft.

            ## Startup Handoff

            When DevCraft launches a terminal AI client, it should provide both this file and `soul.md` so the client knows who it is working for and how DevCraft is organized.

            """;
    }
}
