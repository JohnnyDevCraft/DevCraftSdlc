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

            Fallback project context (use these exact values when configure.json is missing, invalid, or lacks them):
            - ProjectKey: {{request.ProjectConfiguration.ProjectKey}}
            - ProjectProfile.Name: {{request.ProjectConfiguration.ProjectProfile.Name}}
            - ProjectProfile.Purpose: {{request.ProjectConfiguration.ProjectProfile.Purpose}}
            - ProjectProfile.Description: {{request.ProjectConfiguration.ProjectProfile.Description}}

            Repository .devcraft/configure.json must keep ProjectKey, SelectedFeatureStorage, FeaturesIndexPath, and ProjectProfile (Name, Purpose, Description). Never change an existing ProjectKey.

            Rules:
            - Read actual files before changing them.
            - Preserve user-authored content.
            - If repository .devcraft/configure.json is missing or invalid, recreate it from the repository files and the fallback project context in this prompt.
            - Do not destructively delete repository feature folders until their content is preserved in the target location.
            - Both repository-central and system-central feature folders must use GUID folder names.
            - Repository-central feature folders live under repository .devcraft/features/<guid>.
            - System-central feature folders live under profile .DevCraft/features/<guid>.
            - Profile .DevCraft/features/projects.json indexes every feature of every storage type for this project; feature browsing reads it.
            - Repository .devcraft/configure.json Features lists only repo-central features. DevCraft moves any other entry into projects.json and removes it from configure.json the next time it runs here.
            - Ensure feature browsing can list migrated features after upgrade.

            Repository configuration feature records (repo-central only) should include Name, Slug, ShortDescription, FolderName, StorageType, ExternalReference (the work item URL), Status, WorkItemId, and Branch.
            Profile projects index entries should include project id, name, slug, repo-name, repo-remote, working-copies (path, branch, last-seen), and feature records with id, feature-name, slug, description, folder-name, storage-type, work-item-id, external-reference, branch, and status.

            If effective storage is system-central:
            - Move or copy legacy repository feature folders into profile .DevCraft/features/<guid>.
            - Record the features in profile projects.json with StorageType system-central; do not keep them in repository configure.json.

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
