# Feature Research: Beta 11 Repository Upgrade Agent

## Feature Reference

- Feature ID: 0005
- Feature Name: Beta 11 Repository Upgrade Agent
- State: Research

## Research Summary

Beta 11 should change the `devcraft -force` path from install-only behavior into install-or-upgrade behavior. The deterministic DevCraft code should detect versions, decide whether an upgrade is needed, and build a precise non-interactive agent prompt. The terminal agent should perform the repository-specific migration and return a structured result so DevCraft can continue launching after success.

## Repo Evidence

### Existing force install exits too early for existing DevCraft repositories

Evidence: [StartupFlow.cs](/Users/john/Source/repos/DevCraft/src/DevCraft.Cli/StartupFlow.cs)

- `StartupFlow.Run` scans markers and computes `hasDevCraftMarkers`.
- If `hasDevCraftMarkers` is true, it reports `Folder already uses DevCraft` and returns before checking `forceInstall`.
- Result: `devcraft -force` cannot currently upgrade a folder that already contains `.devcraft`.

### Existing installer preserves files but does not version repository installs

Evidence: [DevCraftInstaller.cs](/Users/john/Source/repos/DevCraft/src/DevCraft.Cli/DevCraftInstaller.cs)

- `DevCraftInstaller.Install` creates `.devcraft`, `.devcraft/features`, standard control files, and `.devcraft/configure.json`.
- It uses `WriteIfMissing`, so existing files are preserved.
- It does not create or update `.devcraft/version`.

### System-central feature tracking already has a profile index

Evidence: [SystemCentralProjectsStore.cs](/Users/john/Source/repos/DevCraft/src/DevCraft.Cli/SystemCentralProjectsStore.cs)

- Profile-level system-central tracking lives at profile `features/projects.json`.
- The writer emits project `id`, `name`, `slug`, `repo-location`, `repo-name`, and feature records with id/name/slug/description/folder/storage/work-item/external/status fields.
- This gives the upgrade prompt a concrete current target shape.

### Feature creation already separates repo-central and system-central folders

Evidence: [FeatureCommand.cs](/Users/john/Source/repos/DevCraft/src/DevCraft.Cli/FeatureCommand.cs)

- Repo-central feature creation creates repository `.devcraft/features/<feature-folder>`.
- System-central feature creation updates profile `features/projects.json` and creates profile `features/<feature-folder>`.
- This supports the requested migration direction when a repository is configured for system-central but still contains repository feature folders.

### Current feature folders are intended to be GUIDs

Evidence: [FeatureCommand.cs](/Users/john/Source/repos/DevCraft/src/DevCraft.Cli/FeatureCommand.cs) and [DevCraftFeature.cs](/Users/john/Source/repos/DevCraft/src/DevCraft.Cli/DevCraftFeature.cs)

- New feature creation sets `FolderName` to `Guid.NewGuid().ToString()`.
- `DevCraftFeature` stores `FolderName` independently from `Slug`.
- The normalized Beta 11 target should therefore be GUID folder names for both `repo-central` and `system-central`.
- Storage mode should only control the folder root: repository `.devcraft/features/<guid>` or profile `.DevCraft/features/<guid>`.
- Repository `.devcraft/configure.json` should still carry feature history for the active repository, even when artifacts live in the profile feature folder.
- Profile `.DevCraft/features/projects.json` should also carry project and feature history so DevCraft can remember those features from outside the repository folder.

### Repository feature storage currently exists, but profile default storage does not

Evidence: [FeatureStorageSelector.cs](/Users/john/Source/repos/DevCraft/src/DevCraft.Cli/FeatureStorageSelector.cs), [ProjectDevCraftConfiguration.cs](/Users/john/Source/repos/DevCraft/src/DevCraft.Cli/ProjectDevCraftConfiguration.cs), and [DevCraftProfileConfiguration.cs](/Users/john/Source/repos/DevCraft/src/DevCraft.Cli/DevCraftProfileConfiguration.cs)

- Repository `.devcraft/configure.json` has `SelectedFeatureStorage`.
- `FeatureStorageSelector.EnsureSelected` prompts when repository `SelectedFeatureStorage` is empty.
- Profile `.DevCraft/configure.json` has `FeatureStorageTypes` catalog entries, but no profile-level default feature storage setting.
- Beta 11 should add a profile default and treat repository `SelectedFeatureStorage` as an optional override.

### AI client launching already exists, but upgrade needs a dedicated launcher path

Evidence: [FeatureAiSessionLauncher.cs](/Users/john/Source/repos/DevCraft/src/DevCraft.Cli/FeatureAiSessionLauncher.cs)

- Existing launch code builds a prompt and waits for the configured terminal agent process to exit.
- Upgrade should reuse the supported-client/session model but use a dedicated prompt and result handling so `.devcraft/version` is written only when the upgrade process returns success.
- The prompt must be complete because the upgrade should not require user intervention after `devcraft -force` starts.

## Recommended Design

### Deterministic DevCraft responsibilities

- Determine running DevCraft semantic version from assembly metadata.
- Read repository `.devcraft/version` if present.
- Compare repository version with running version using prerelease-aware semantic comparison.
- Install missing `.devcraft` when force is used in a non-DevCraft folder.
- Launch an upgrade agent when repository `.devcraft/version` is missing, invalid, or older.
- Read the upgrade agent result and write `.devcraft/version` only after successful install or upgrade.
- Report clear status when no upgrade is needed, no supported agent is available, or the agent fails.
- Continue normal menu startup after a successful upgrade.
- Resolve the effective feature storage mode from repository override first, then profile default.
- Pass the effective feature storage mode into prompts and agent handoffs.

### Agent responsibilities

- Inspect repository `.devcraft` files and profile `.DevCraft` files.
- Preserve existing user-authored files.
- Add missing current DevCraft control files when safe.
- Migrate repository feature folders to profile feature storage when repository configuration says `system-central`.
- Normalize legacy feature folders to GUID folder names for both repository-central and system-central storage.
- Update profile `features/projects.json` to current shape.
- Update repository configuration feature records to current storage information.
- Keep repository and profile project/feature history aligned after upgrade.
- Preserve repository-level storage overrides during upgrade, and use the effective storage mode for migration decisions.
- Return a structured result indicating success/failure, moved features, skipped features, and summary text.
- Leave a clear textual summary of what changed and what was skipped.

## Risks

- Agent-driven migration can make destructive mistakes if the prompt is vague. The prompt must explicitly require preservation and inspection before changes.
- Version comparison for prereleases can be subtly wrong if treated as plain strings. Tests must include beta 9, beta 10, and beta 11 ordering.
- Writing `.devcraft/version` before the agent succeeds would mask failed upgrades. DevCraft must write it only after success.
- Existing repositories may have partial or hand-edited DevCraft files. The agent prompt should prefer best-effort migration and clear reporting over forced normalization.
- If the agent does not return machine-readable success/failure, DevCraft could incorrectly continue. The result contract should be explicit and tested.
- If profile default and repository override semantics are unclear, prompts may use the wrong artifact storage location. The effective storage mode should be calculated once and passed consistently.

## Validation Direction

- Add focused startup tests for force install in missing/current/older repository version scenarios.
- Add focused tests for version file writing and prerelease comparison.
- Add focused tests for upgrade prompt content, especially effective storage resolution, system-central migration guidance, repo-central GUID normalization, dual repository/profile history updates, and result contract instructions.
- Keep unit tests agent-safe by using a fake upgrade launcher that records prompts and returns success/failure.
