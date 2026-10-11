namespace DevCraft.Cli;

public static class DevCraftRepositoryUpgradePromptBuilder
{
    public static string Build(DevCraftRepositoryUpgradeRequest request)
    {
        string repositoryConfigurationPath = ProjectDevCraftConfigurationStore.PathFor(request.Context.CurrentDirectory);
        string profileProjectsPath = SystemCentralProjectsStore.PathFor(request.Context.ProfileDirectory);
        string repositoryVersion = string.IsNullOrWhiteSpace(request.RepositoryVersion)
            ? "missing or invalid"
            : request.RepositoryVersion;

        return $$"""
            You are upgrading an existing DevCraft repository installation.

            This upgrade is non-interactive. Do not ask the operator questions. Inspect the files, make the safest best-effort upgrade decisions, and return a structured result.

            Versions:
            - Current DevCraft version: {{request.CurrentVersion}}
            - Repository DevCraft version: {{repositoryVersion}}

            Paths:
            - Active repository/codebase for this session: {{request.Context.CurrentDirectory}}
            - Profile DevCraft folder: {{request.Context.ProfileDirectory}}
            - Repository configuration: {{repositoryConfigurationPath}}
            - Profile projects index: {{profileProjectsPath}}

            Effective feature storage for this repository: {{request.EffectiveFeatureStorage}}

            Rules:
            - Read actual files before changing them.
            - Preserve user-authored content.
            - If repository .devcraft/configure.json is missing or invalid, recreate it from the repository files and the fallback project context in this prompt.
            - Do not destructively delete repository feature folders until their content is preserved in the target location.
            - Both repository-central and system-central feature folders must use GUID folder names.
            - Repository-central feature folders live under repository .devcraft/features/<guid>.
            - System-central feature folders live under profile .DevCraft/features/<guid>.
            - Keep repository .devcraft/configure.json feature history and profile .DevCraft/features/projects.json feature history aligned.
            - Repository .devcraft/configure.json is the source for the active codebase when DevCraft is run in this folder.
            - Profile .DevCraft/features/projects.json is the cross-folder project and feature history index.
            - Ensure feature browsing can list migrated features after upgrade.

            Repository configuration feature records should include Name, Slug, ShortDescription, FolderName, StorageType, ExternalReference, and Status.
            Profile projects index entries should include project id, name, slug, repo-location, repo-name, and feature records with id, feature-name, slug, description, folder-name, storage-type, work-item-id, external-reference, and status.

            If effective storage is system-central:
            - Move or copy legacy repository feature folders into profile .DevCraft/features/<guid>.
            - Update repository feature records so StorageType is system-central.
            - Update profile projects.json with the same feature identities.

            If effective storage is repo-central:
            - Keep feature artifacts under repository .devcraft/features/<guid>.
            - Update repository feature records so StorageType is repo-central.
            - Update profile projects.json with the same project and feature history even though artifacts remain repository-local.

            Return only JSON with this shape:
            {
              "success": true,
              "summary": "what changed",
              "movedFeatures": ["feature names or ids moved"],
              "skippedFeatures": ["feature names or ids skipped with reason"]
            }
            """;
    }
}
