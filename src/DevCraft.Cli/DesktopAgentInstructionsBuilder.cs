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
            1. In the repository, read .devcraft/AGENT.md and .devcraft/configure.json. Use ProjectKey and SelectedFeatureStorage (the storage new features get). Its Features list holds only repo-central features.
            2. Open {Path.Combine(featuresDirectory, "projects.json")} and find the project whose "id" equals ProjectKey. That file indexes every feature of the project, whatever its storage type. Each feature has "slug", "folder-name", "storage-type", "status", "work-item-id" (the tracker ID), "external-reference" (the work item URL), and "branch" (the git branch it is worked on). The project lists its "working-copies" (path, branch, last-seen) and "repo-remote".
            3. Read {Path.Combine(profileDirectory, "feature-storage", "<storage-type>.md")} for where that feature's storage type keeps its artifacts.
            4. For system-central features, read the artifacts in {Path.Combine(featuresDirectory, "<folder-name>")} (spec.md first). For repo-central features, read them under the repository's .devcraft/features/<folder-name> folder.
            5. When you change a feature's status, edit only that "status" value in that file, and re-read it right before editing; other DevCraft sessions may be writing it.
            """;
    }
}
