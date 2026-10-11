namespace DevCraft.Cli;

public static class DesktopAgentInstructionsBuilder
{
    public static string Build(string profileDirectory)
    {
        string[] parts =
        [
            BuildIdentity(profileDirectory),
            SituationPromptContextBuilder.Build(profileDirectory),
            BuildDormantDevCraft(profileDirectory),
        ];

        return string.Join(
            "\n\n",
            parts
                .Select(part => part.ReplaceLineEndings("\n").Trim())
                .Where(part => part.Length > 0));
    }

    private static string BuildIdentity(string profileDirectory)
    {
        return $"""
            Start every conversation by reading {Path.Combine(profileDirectory, "soul.md")}. It tells you who you are working for and who you should be. Adopt that identity from your first reply.

            If you cannot read a file named in these instructions, say so plainly and do not act as though you had read it.
            """;
    }

    private static string BuildDormantDevCraft(string profileDirectory)
    {
        string featuresDirectory = Path.Combine(profileDirectory, "features");

        return $"""
            DevCraft (dormant):
            Read these files now so you know DevCraft, but do NOT apply any of their rules yet:
            - {Path.Combine(profileDirectory, "DevCraft.md")}: the DevCraft operating rules.
            - {Path.Combine(profileDirectory, "initialized.md")}: how DevCraft is organized.
            - {Path.Combine(profileDirectory, "configure.json")}: the catalog of skills, standards, architectures, templates, project types, and feature storage types. Each entry has Slug, Name, Description, and a Path relative to {profileDirectory}.

            Until the operator says "Activate DevCraft", behave as an ordinary assistant with the identity above and any situational awareness given above. A repository that contains a .devcraft folder does not activate DevCraft; do not enforce DevCraft states, gates, or workflow before that prompt.

            When the operator says "Activate DevCraft", apply the DevCraft rules for the rest of this thread only. Then find feature information this way:
            1. In the repository, read .devcraft/AGENT.md and .devcraft/configure.json. Use ProjectKey, SelectedFeatureStorage, and the Features list (Name, Slug, FolderName, Status).
            2. Read {Path.Combine(profileDirectory, "feature-storage", "<SelectedFeatureStorage>.md")} for where that storage type keeps feature artifacts.
            3. For system-central storage, open {Path.Combine(featuresDirectory, "projects.json")}, find the project whose "id" equals ProjectKey, find the feature in its "features" list ("slug", "folder-name", "status"), and read its artifacts in {Path.Combine(featuresDirectory, "<folder-name>")} (spec.md first).
            4. For repo-central storage, read the artifacts under the repository's .devcraft/features/ folder.
            """;
    }
}
