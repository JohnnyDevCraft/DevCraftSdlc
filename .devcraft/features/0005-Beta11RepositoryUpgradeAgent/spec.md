# Feature Spec: Beta 11 Repository Upgrade Agent

## Feature Reference

- Feature ID: 0005
- Feature Name: Beta 11 Repository Upgrade Agent
- Type: Feature
- State: Discovery

Valid states: `Discovery`, `Clarification`, `Research`, `Planning`, `Analysis`, `Implementation`, `Complete`.
Code is only allowed while state is `Implementation`.

## Work Item

- Work Item Source: Operator conversation
- Work Item ID: N/A
- Work Item Title: Upgrade existing repository DevCraft installations with an agent
- Work Item Summary: `devcraft -force` should upgrade existing repository `.devcraft` installations, record the installed DevCraft version, and use an AI agent to migrate older repository feature storage into the current DevCraft layout when needed.

## Overview

DevCraft currently treats a folder that already contains `.devcraft` as already using DevCraft and exits early. That is too weak for Beta 11 because older repository control folders may need newer runtime metadata, feature storage migration, and current workflow layout.

Beta 11 adds a repository upgrade flow. Each repository `.devcraft` folder will contain a plain `version` file containing only the current DevCraft semantic version, for example:

```text
1.0.0-beta.11
```

When `devcraft -force` runs in a folder that already has `.devcraft`, DevCraft should inspect the existing install, compare the repository version with the running DevCraft version, and upgrade the repository when the repository version is missing or older.

The upgrade should be performed by an AI agent without user intervention after `devcraft -force` starts. DevCraft should collect deterministic context, build a complete upgrade prompt, launch the selected/default supported terminal agent in the target repository, wait for the agent to perform the upgrade, read the returned upgrade result, and then continue launching DevCraft when the upgrade succeeds.

## Goals

- Add `.devcraft/version` to repository DevCraft installations.
- Make `devcraft -force` upgrade existing `.devcraft` installations when the stored repository version is missing or older than the running DevCraft version.
- Preserve existing repository DevCraft content unless an upgrade migration intentionally moves it.
- If the repository is configured for system feature storage but features are still physically stored in repository `.devcraft/features`, migrate those features into the current system-storage shape.
- Normalize feature storage so every feature uses a GUID folder name in both repository-central and system-central modes.
- Write project and feature history to both the repository DevCraft configuration and the profile DevCraft project index so DevCraft can discover those features from the repository folder and from the profile-level view.
- Add a profile-level default feature storage setting for DevCraft as a whole.
- Allow each repository to optionally override the profile default feature storage setting.
- Use an AI agent to perform the upgrade so it can inspect the actual repository, make context-aware decisions, and return a structured upgrade result.
- Record successful upgrades by writing the current version into `.devcraft/version`.
- Keep failed or incomplete upgrades from pretending the repository is current.
- Continue normal DevCraft startup after successful upgrade so browsing features for the repository shows the migrated feature list.

## Non-Goals

- Do not implement a full deterministic migration engine for every historical DevCraft layout in Beta 11.
- Do not delete repository feature folders before the agent has preserved or moved them.
- Do not change root `AGENT.md` or `AGENTS.md` during upgrade unless the agent determines the file is part of the repository's own DevCraft control surface and the prompt explicitly allows it.
- Do not fetch, install, or update the DevCraft binary itself. This feature upgrades repository control files, not the local executable.
- Do not require user choices or interactive operator intervention during the upgrade.

## Current State

`StartupFlow.Run` detects DevCraft markers and exits before force install logic runs. This means `devcraft -force` does not upgrade a repository that already has `.devcraft`.

`DevCraftInstaller.Install` creates `.devcraft`, `.devcraft/features`, control Markdown files, and `.devcraft/configure.json`, preserving existing files. It does not create `.devcraft/version`.

Feature storage can be `repo-central` or `system-central`. `repo-central` feature folders live under repository `.devcraft/features`. `system-central` tracking lives in the profile index at profile `.DevCraft/features/projects.json`, and system feature folders live under profile `.DevCraft/features/<folder-name>`.

Feature AI launching already exists for feature handoffs, but there is no repository upgrade-specific prompt or launcher path.

Current repository configuration already has `SelectedFeatureStorage`, but there is no profile-level default feature storage setting. When repository `SelectedFeatureStorage` is null, DevCraft currently prompts for storage selection instead of using a default.

## Target State

When DevCraft installs or upgrades repository DevCraft control files, it writes `.devcraft/version` with the running DevCraft semantic version without build metadata.

When `devcraft -force` runs:

1. DevCraft initializes the profile folder as usual.
2. DevCraft detects whether the current folder has repository `.devcraft` markers.
3. If the repository does not have `.devcraft`, DevCraft performs the existing force-install flow and writes `.devcraft/version`.
4. If the repository has `.devcraft`, DevCraft reads `.devcraft/version`.
5. If the version is equal to the running version, DevCraft reports that the repository already uses the current DevCraft version and does not launch an upgrade agent.
6. If the version is missing, invalid, or older than the running version, DevCraft launches the repository upgrade agent flow.
7. The upgrade agent receives a prompt that describes the current DevCraft version, the detected repository version, repository path, profile path, repository configuration path, profile projects index path, storage mode, and migration rules.
8. The upgrade agent inspects the repository, performs the migration, and returns a structured upgrade result.
9. If the agent exits successfully and returns a successful upgrade result, DevCraft writes `.devcraft/version` with the current running version.
10. If the agent fails, cannot be launched, or returns a failed upgrade result, DevCraft reports the failure and leaves `.devcraft/version` unchanged.
11. After a successful upgrade, DevCraft continues normal startup and launches the menu instead of stopping at the upgrade result.

All feature records must use GUID folder names. This applies to both `repo-central` and `system-central`. The only storage-mode difference is where the GUID-named folder lives:

- `repo-central`: feature folders live under repository `.devcraft/features/<guid>`.
- `system-central`: feature folders live under profile `.DevCraft/features/<guid>`.

When upgrading legacy feature folders that are not GUID-named, the agent must create new GUIDs, move or copy each legacy feature folder into the correct GUID-named folder location, and update the relevant feature catalog records to point at the new folder names.

Project and feature history must be written in two places:

- Repository-local history in repository `.devcraft/configure.json`, because when DevCraft is run in a folder it assumes that folder is the active codebase for the session.
- Profile-level history in profile `.DevCraft/features/projects.json`, because DevCraft should also know about projects and features from outside the repository folder.

The storage mode controls where feature artifact folders live, not whether the repository or profile catalogs know about the feature. Both catalogs should be kept in sync enough for feature browsing and handoff context.

Feature storage selection must work in two layers:

- Profile default: profile `.DevCraft/configure.json` stores the default feature storage mechanism for DevCraft as a whole.
- Repository override: repository `.devcraft/configure.json` may store a repository-level feature storage override.

When the repository override is not set, DevCraft uses the profile default. When the repository override is set, DevCraft uses the repository override for that repository. DevCraft should read the repository configuration at startup to determine the effective storage mode for the current working directory, and prompts should receive that effective storage mode.

Configure DevCraft must include a `Change Repository-Level Feature Storage` action. That action changes only the current repository override. It must also support returning to the profile default by clearing the repository override.

If a repository is configured with `SelectedFeatureStorage` = `system-central` but features are still present in repository `.devcraft/features`, the upgrade prompt must instruct the agent to:

- Preserve feature folder content before removing or moving anything.
- Ensure every moved feature has a current entry in profile `.DevCraft/features/projects.json`.
- Ensure every moved feature also has a current entry in repository `.devcraft/configure.json`.
- Ensure the system-central project entry records project id, name, slug, repo location, repo name, and feature records in the current `projects.json` shape.
- Generate GUID folder names when legacy feature folders are not already GUIDs.
- Move or copy repository feature folders into profile `.DevCraft/features/<guid>`.
- Update repository `.devcraft/configure.json` feature records so their storage type matches `system-central`.
- Ensure feature browsing can list the migrated features from the repository configuration and profile projects index after upgrade.
- Leave a clear summary in the agent response of what moved and what could not be confidently moved.

If a repository is configured with `SelectedFeatureStorage` = `repo-central`, the upgrade prompt must instruct the agent to:

- Keep feature content under repository `.devcraft/features`.
- Generate GUID folder names when legacy feature folders are not already GUIDs.
- Move or copy legacy feature folders into repository `.devcraft/features/<guid>`.
- Update repository `.devcraft/configure.json` feature records so `FolderName` values match the GUID folders and `StorageType` values match `repo-central`.
- Update profile `.DevCraft/features/projects.json` with the same project and feature history even though artifacts remain repository-local.
- Ensure feature browsing can list the migrated features from the repository configuration after upgrade.

## Requirements

- Requirement 1: Repository DevCraft installs must create `.devcraft/version`.
- Requirement 2: `.devcraft/version` must contain only the current semantic DevCraft version, with no prefix, no build metadata, and no extra text.
- Requirement 3: `devcraft -force` in a non-DevCraft folder must continue to install DevCraft.
- Requirement 4: `devcraft -force` in a current-version DevCraft folder must not reinstall or launch an upgrade agent.
- Requirement 5: `devcraft -force` in a missing-version DevCraft folder must treat the repository as legacy and launch the upgrade agent flow.
- Requirement 6: `devcraft -force` in an older-version DevCraft folder must launch the upgrade agent flow.
- Requirement 7: `devcraft -force` must leave `.devcraft/version` unchanged when the upgrade agent cannot be launched or exits unsuccessfully.
- Requirement 8: `devcraft -force` must write the current version after a successful upgrade agent run.
- Requirement 9: Version comparison must support prerelease values such as `1.0.0-beta.9`, `1.0.0-beta.10`, and `1.0.0-beta.11`.
- Requirement 10: The upgrade agent prompt must include the current running DevCraft version and detected repository DevCraft version.
- Requirement 11: The upgrade agent prompt must include the repository path, profile path, repository configuration path, and profile projects index path.
- Requirement 12: The upgrade agent prompt must instruct the agent to inspect actual files before changing them.
- Requirement 13: The upgrade agent prompt must instruct the agent to preserve existing user-authored content and avoid destructive deletion unless replacement/migration is confirmed.
- Requirement 14: The upgrade agent prompt must include system-central migration guidance for repository-stored features.
- Requirement 15: When repository configuration uses `system-central`, repository feature folders must be moved or copied into the profile feature store by the upgrade agent.
- Requirement 16: The system-central profile projects index must be updated to match the current DevCraft feature tracking shape.
- Requirement 17: Repository feature records must match the current configured storage mode after upgrade.
- Requirement 18: DevCraft must report a clear status when no installed/supported agent is available for upgrade.
- Requirement 19: Tests must prove legacy/missing-version force upgrade launches the upgrade agent and writes version only after success.
- Requirement 20: Tests must prove current-version force upgrade does not launch the upgrade agent.
- Requirement 21: The upgrade must not require interactive operator input after `devcraft -force` starts.
- Requirement 22: The upgrade agent must return a structured upgrade result that DevCraft can use to decide success or failure.
- Requirement 23: DevCraft must continue normal startup after a successful upgrade.
- Requirement 24: After successful upgrade, feature browsing must show migrated features for the repository.
- Requirement 25: Both repository-central and system-central feature folders must use GUID folder names.
- Requirement 26: When upgrading non-GUID legacy feature folders, the upgrade agent must create GUID folder names and update feature records to point at those folders.
- Requirement 27: When repository configuration uses `repo-central`, migrated feature content must remain under repository `.devcraft/features/<guid>`.
- Requirement 28: When repository configuration uses `system-central`, migrated feature content must live under profile `.DevCraft/features/<guid>`.
- Requirement 29: The upgrade prompt must train the agent on the current repository feature catalog and profile projects index shapes.
- Requirement 30: The upgrade prompt must train the agent on how to create or preserve GUID feature folder names.
- Requirement 31: The upgrade must write project and feature history to repository `.devcraft/configure.json`.
- Requirement 32: The upgrade must write project and feature history to profile `.DevCraft/features/projects.json`.
- Requirement 33: Repository and profile feature history must describe the same feature identities after upgrade.
- Requirement 34: DevCraft must continue to treat the current working directory as the active codebase for the session, even when profile-level history contains other repositories.
- Requirement 35: Profile `.DevCraft/configure.json` must support a default feature storage setting for DevCraft as a whole.
- Requirement 36: Repository `.devcraft/configure.json` must support an optional repository-level feature storage override.
- Requirement 37: When the repository override is null or empty, DevCraft must use the profile default feature storage setting.
- Requirement 38: When the repository override is set, DevCraft must use that override for the current repository.
- Requirement 39: Configure DevCraft must include `Change Repository-Level Feature Storage`.
- Requirement 40: `Change Repository-Level Feature Storage` must change only the current repository override and must not change the profile default.
- Requirement 41: `Change Repository-Level Feature Storage` must allow clearing the repository override so the repository returns to the profile default.
- Requirement 42: DevCraft prompts and agent handoffs must include the effective feature storage mode for the current repository.

## Open Questions

- None recorded. The operator requested an agent-driven best-effort upgrade flow for Beta 11.

## Clarification Log

### CL-001

- Question: What should Beta 11 do when `devcraft -force` runs in an older repository DevCraft install?
- Answer: It should upgrade the repository to the current DevCraft version, write `.devcraft/version`, and use an agent to perform upgrade decisions.
- Recommendation: Use deterministic version detection and prompt construction in DevCraft, then let the selected/default terminal agent perform repository-specific migration work.

### CL-002

- Question: Should the upgrade ask the operator questions while running?
- Answer: No. DevCraft should create the prompt, the agent should do the upgrade and return the result, and DevCraft should continue launching after success.
- Recommendation: Make the upgrade non-interactive after `devcraft -force`; treat missing or failed agent results as upgrade failures.

### CL-003

- Question: Should feature folder GUID behavior differ between repository-central and system-central storage?
- Answer: No. Both storage modes should use GUID folder names. The only difference is whether those GUID folders live under the repository `.devcraft/features` folder or the profile `.DevCraft/features` folder.
- Recommendation: Teach the upgrade agent to create GUID folder names for legacy features and update both repository and profile catalogs as appropriate.

### CL-004

- Question: Should feature history be repository-only or profile-only after upgrade?
- Answer: Neither. Project and feature history should be written to both repository `.devcraft/configure.json` and profile `.DevCraft/features/projects.json`.
- Recommendation: Treat artifact storage location as separate from history/catalog visibility; keep both catalogs aligned so DevCraft can browse features from the repository folder and remember them from the profile-level index.

### CL-005

- Question: How should default feature storage and repository-specific feature storage interact?
- Answer: DevCraft should have a profile-level default feature storage mechanism. Each repository can optionally override that default. If no repository override is set, the repository uses the profile default.
- Recommendation: Add profile `DefaultFeatureStorage`, keep repository `SelectedFeatureStorage` as an override, rename the Configure DevCraft action to `Change Repository-Level Feature Storage`, and pass the effective storage mode to all prompts.
