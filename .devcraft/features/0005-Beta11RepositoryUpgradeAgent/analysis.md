# Feature Analysis: Beta 11 Repository Upgrade Agent

## Feature Reference

- Feature ID: 0005
- Feature Name: Beta 11 Repository Upgrade Agent
- Spec: [spec.md](./spec.md)
- Research: [research.md](./research.md)
- Tasks: [tasks.md](./tasks.md)
- State: Analysis

## Analysis Summary

The requested Beta 11 behavior is implementable with the existing startup, installer, supported-client, and system-central feature tracking infrastructure. The primary design concern is safety: DevCraft should make deterministic upgrade/no-upgrade decisions and let the agent handle repository-specific migration, but DevCraft must not mark a repository as upgraded unless the agent actually succeeds and returns a usable result.

## Consistency Check

- The spec requires `.devcraft/version`; tasks include a repository version store and install-time write.
- The spec requires `devcraft -force` to upgrade existing installs; tasks move force handling before the existing-DevCraft early return path.
- The spec requires agent-driven upgrade; tasks add a testable upgrade-agent launcher and prompt builder.
- The spec requires profile default plus repository override feature storage; tasks add profile `DefaultFeatureStorage`, effective storage resolution, and a repository-level Configure DevCraft action.
- The spec requires repo-stored features to move to system-central when configured; tasks add system-central prompt detection and migration instructions.
- The spec requires GUID feature folders in both storage modes; tasks add GUID normalization guidance for repo-central and system-central.
- The spec requires no user intervention after `devcraft -force`; tasks require a complete non-interactive prompt and structured result contract.
- The spec requires repository and profile feature history; tasks require the agent prompt to update both repository `.devcraft/configure.json` and profile `.DevCraft/features/projects.json`.
- The spec requires Beta 11 metadata; tasks include package/docs/release-selection updates.

## Resolved Issues

### AN-001: Should DevCraft do migration itself or delegate migration to an agent?

- Risk: A deterministic migrator would be safer for simple known layouts, but it may be brittle against hand-edited or older DevCraft folders.
- Resolution: DevCraft should own version detection, prompt construction, and success/failure gating. The terminal agent should own the repository-specific migration steps.
- Reason: This matches the operator's explicit requirement that the upgrade should be done by an agent that can make decisions while upgrading.

### AN-002: When should `.devcraft/version` be updated?

- Risk: Writing the new version before migration completes would hide failed upgrades.
- Resolution: New installs write the current version immediately. Existing legacy/older installs write the current version only after the upgrade agent exits successfully and returns success.
- Reason: The version file is the upgrade completion marker.

### AN-003: What should happen when no agent is available?

- Risk: Silent no-op behavior would leave the operator thinking the repository upgraded.
- Resolution: DevCraft should report a clear status, leave `.devcraft/version` unchanged, and not claim success.
- Reason: Agent-driven upgrade is mandatory for this feature.

### AN-004: Should repository and profile feature history both be maintained?

- Risk: If history is only profile-level, DevCraft may not list features when run in the repository folder. If history is only repository-level, DevCraft loses cross-folder memory.
- Resolution: Upgrade should maintain both repository `.devcraft/configure.json` and profile `.DevCraft/features/projects.json` project/feature history.
- Reason: DevCraft assumes the current working directory is the active codebase for the session, but profile-level history should still know about repositories and features outside the current folder.

### AN-005: Should upgrades ask the operator for decisions?

- Risk: Interactive questions would interrupt the startup path and prevent unattended upgrades from completing.
- Resolution: The prompt must contain all upgrade instructions; the agent performs the upgrade and returns a structured result.
- Reason: The operator clarified that DevCraft should create the prompt, the agent should do the upgrade, and DevCraft should continue launching after completion.

### AN-006: Should repository-central and system-central feature folders use different naming schemes?

- Risk: Different folder naming schemes would make feature migration and browsing harder to reason about.
- Resolution: Both storage modes should use GUID folder names. Storage mode only changes the root folder location.
- Reason: Current new feature creation already uses GUIDs for `FolderName`, and the operator clarified that the only intended difference is repository versus profile storage location.

### AN-007: Should feature storage be only global or only repository-local?

- Risk: A global-only setting prevents one-off repository needs such as repo-local features or ADO-backed features. A repository-only setting forces repeated setup and removes the convenience of a DevCraft-wide default.
- Resolution: Add a profile-level default feature storage setting and keep repository `SelectedFeatureStorage` as an optional override.
- Reason: The operator wants most repositories to inherit the default while allowing specific repositories to choose repo-central, system-central, ADO, or another supported storage type.

## Implementation Readiness

The feature is ready for operator review. It should not be implemented until the operator explicitly authorizes making feature `0005` active and moving it into `Implementation`.
