# Feature Tasks: Beta 11 Repository Upgrade Agent

## Feature Reference

- Feature ID: 0005
- Feature Name: Beta 11 Repository Upgrade Agent
- Spec: [spec.md](./spec.md)
- Research: [research.md](./research.md)
- State: Planning

## Planning Summary

- Planning goal: Add non-interactive, versioned repository DevCraft upgrades through `devcraft -force`.
- Recommended implementation sequence: profile default feature storage first, then version services, then force-upgrade detection, then upgrade-agent prompt/launcher, then feature catalog/GUID migration prompt coverage, then Beta 11 release metadata.
- Known dependencies: `StartupFlow`, `DevCraftInstaller`, `ProjectDevCraftConfigurationReader`, `ProfileConfigurationReader`, `FeatureStorageSelector`, `SystemCentralProjectsStore`, supported terminal client configuration, and feature handoff launcher patterns.
- TDD approach: Add focused red tests per phase, implement the smallest behavior to pass, then run focused and full validation.

## Phases

### Phase 1: Profile Default And Repository Override Feature Storage

- Purpose: Let DevCraft use a profile-level feature storage default while allowing each repository to override it.
- Expected Outcome: Feature workflows use the effective storage mode for the current repository, and Configure DevCraft can change or clear the repository override.
- Entry Criteria: Operator approves Beta 11 implementation.
- Exit Criteria: Storage default/override tests pass.

#### Application Development

- [ ] TASK-001 Add `DefaultFeatureStorage` to the profile configuration model.
- [ ] TASK-002 Default profile `DefaultFeatureStorage` to `system-central` unless the operator chooses otherwise.
- [ ] TASK-003 Treat repository `SelectedFeatureStorage` as an optional repository override.
- [ ] TASK-004 Add an effective feature storage resolver that prefers repository override and falls back to profile default.
- [ ] TASK-005 Change feature creation/listing workflows to use the effective storage mode.
- [ ] TASK-006 Rename Configure DevCraft `Change Feature Storage` to `Change Repository-Level Feature Storage`.
- [ ] TASK-007 Allow `Change Repository-Level Feature Storage` to clear the repository override and return to the profile default.
- [ ] TASK-008 Include effective feature storage in feature and upgrade prompts.

#### Tests And Validation

- [ ] TEST-001 Verify profile configuration includes `DefaultFeatureStorage`.
- [ ] TEST-002 Verify the default profile feature storage is `system-central`.
- [ ] TEST-003 Verify repository override wins over profile default.
- [ ] TEST-004 Verify missing repository override uses profile default.
- [ ] TEST-005 Verify Configure DevCraft includes `Change Repository-Level Feature Storage`.
- [ ] TEST-006 Verify changing repository-level feature storage updates repository configuration only.
- [ ] TEST-007 Verify clearing repository-level feature storage restores profile-default behavior.
- [ ] TEST-008 Verify prompts include the effective feature storage mode.

### Phase 2: Repository Version File

- Purpose: Record and compare the DevCraft version installed in each repository `.devcraft` folder.
- Expected Outcome: New installs write `.devcraft/version`, and tests can compare repository versions against the running DevCraft version.
- Entry Criteria: Phase 1 exists.
- Exit Criteria: Version file tests and installer tests pass.

#### Application Development

- [ ] TASK-009 Add a reusable current-version provider that returns the visible semantic DevCraft version without build metadata.
- [ ] TASK-010 Add a repository version store for `.devcraft/version`.
- [ ] TASK-011 Write `.devcraft/version` during new repository install.
- [ ] TASK-012 Add prerelease-aware version comparison for Beta versions.
- [ ] TASK-013 Treat missing or invalid repository version as legacy.

#### Tests And Validation

- [ ] TEST-009 Verify new install writes `.devcraft/version`.
- [ ] TEST-010 Verify `.devcraft/version` contains only `1.0.0-beta.11`.
- [ ] TEST-011 Verify beta 10 sorts after beta 9.
- [ ] TEST-012 Verify beta 11 sorts after beta 10.
- [ ] TEST-013 Verify missing version is treated as legacy.
- [ ] TEST-014 Verify invalid version is treated as legacy.

### Phase 3: Force Upgrade Startup Flow

- Purpose: Make `devcraft -force` upgrade existing DevCraft repositories instead of exiting early.
- Expected Outcome: Existing current repositories are skipped, legacy/older repositories invoke upgrade, and non-DevCraft folders still install normally.
- Entry Criteria: Phase 1 exists.
- Exit Criteria: Startup tests cover force behavior for missing, current, older, failed, and non-DevCraft cases.

#### Application Development

- [ ] TASK-014 Move force handling before the early existing-DevCraft return path.
- [ ] TASK-015 Detect current-version repositories and report no upgrade needed.
- [ ] TASK-016 Detect missing-version repositories and request upgrade.
- [ ] TASK-017 Detect older-version repositories and request upgrade.
- [ ] TASK-018 Preserve existing non-DevCraft force install behavior.
- [ ] TASK-019 Write `.devcraft/version` only after successful force install or force upgrade.

#### Tests And Validation

- [ ] TEST-015 Verify `devcraft -force` in a non-DevCraft folder installs and writes version.
- [ ] TEST-016 Verify `devcraft -force` in a current-version DevCraft folder does not launch an upgrade agent.
- [ ] TEST-017 Verify `devcraft -force` in a missing-version DevCraft folder launches an upgrade agent.
- [ ] TEST-018 Verify `devcraft -force` in an older-version DevCraft folder launches an upgrade agent.
- [ ] TEST-019 Verify failed upgrade leaves `.devcraft/version` unchanged.
- [ ] TEST-020 Verify successful upgrade writes the current version.

### Phase 4: Upgrade Agent Handoff

- Purpose: Launch an AI agent with enough precise guidance to perform context-aware repository upgrades safely.
- Expected Outcome: DevCraft selects an available supported client, builds an upgrade-specific prompt, waits for the agent, and records success/failure.
- Entry Criteria: Phase 2 knows when an upgrade is needed.
- Exit Criteria: Prompt tests prove the agent receives all required paths, versions, and safety rules.

#### Application Development

- [ ] TASK-020 Add an upgrade-agent launcher interface for testable startup flow.
- [ ] TASK-021 Reuse supported terminal client configuration to select the upgrade agent.
- [ ] TASK-022 Build an upgrade prompt with current version, repository version, repository path, profile path, repository configuration path, profile projects index path, and effective feature storage mode.
- [ ] TASK-023 Include safety instructions to inspect files before edits and preserve user-authored content.
- [ ] TASK-024 Include success/failure reporting expectations in the prompt.
- [ ] TASK-025 Report a clear status when no supported agent can be launched.
- [ ] TASK-026 Define a structured upgrade result contract with success/failure, moved features, skipped features, and summary text.
- [ ] TASK-027 Continue normal DevCraft startup after a successful upgrade result.
- [ ] TASK-028 Keep the upgrade flow non-interactive after `devcraft -force` starts.

#### Tests And Validation

- [ ] TEST-021 Verify upgrade prompt includes current and repository versions.
- [ ] TEST-022 Verify upgrade prompt includes repository/profile/config/index paths.
- [ ] TEST-023 Verify upgrade prompt includes effective feature storage mode.
- [ ] TEST-024 Verify upgrade prompt includes inspect-before-editing and preservation rules.
- [ ] TEST-025 Verify no-agent cases report a clear status and do not write version.
- [ ] TEST-026 Verify successful fake agent exit writes version.
- [ ] TEST-027 Verify failed fake agent exit does not write version.
- [ ] TEST-028 Verify successful upgrade continues to the normal menu/startup path.
- [ ] TEST-029 Verify the upgrade prompt requires a structured result.
- [ ] TEST-030 Verify the upgrade prompt forbids interactive operator questions during the upgrade.

### Phase 5: Feature Catalog And GUID Migration Guidance

- Purpose: Give the agent exact instructions for normalizing feature folders and catalogs for repository-central and system-central storage.
- Expected Outcome: Upgrade prompts describe GUID folder names, repository configuration records, system-central target shape, dual history updates, and migration rules.
- Entry Criteria: Phase 3 prompt builder exists.
- Exit Criteria: Tests prove prompt content covers GUID folder normalization, repository feature folders, profile projects index updates, repository configuration updates, and feature browsing readiness.

#### Application Development

- [ ] TASK-018 Detect repository configuration `SelectedFeatureStorage`.
- [ ] TASK-019 Detect repository `.devcraft/features` feature folders.
- [ ] TASK-029 Train the agent that feature folder names must be GUIDs in both storage modes.
- [ ] TASK-030 Train the agent to create GUID folder names for legacy non-GUID feature folders.
- [ ] TASK-031 Add repo-central migration instructions that keep feature folders under repository `.devcraft/features/<guid>`.
- [ ] TASK-032 Add system-central migration instructions that move or copy feature folders to profile `.DevCraft/features/<guid>`.
- [ ] TASK-033 Include repository `.devcraft/configure.json` feature catalog shape in the prompt.
- [ ] TASK-034 Include profile `.DevCraft/features/projects.json` target shape in the prompt.
- [ ] TASK-035 Instruct the agent to update repository feature records to match the selected storage mode.
- [ ] TASK-036 Instruct the agent to keep repository `.devcraft/configure.json` feature history and profile `.DevCraft/features/projects.json` feature history aligned.
- [ ] TASK-037 Instruct the agent that the current working directory remains the active codebase for the session.
- [ ] TASK-038 Instruct the agent to ensure feature browsing can list migrated features after upgrade.
- [ ] TASK-039 Instruct the agent to summarize moved/skipped features.

#### Tests And Validation

- [ ] TEST-031 Verify system-central prompt includes repository feature folder migration guidance.
- [ ] TEST-032 Verify system-central prompt includes profile projects index target shape.
- [ ] TEST-033 Verify repo-central repositories do not receive system-central move instructions.
- [ ] TEST-034 Verify prompt tells the agent to preserve feature content before removing repository copies.
- [ ] TEST-035 Verify prompt trains the agent that both storage modes use GUID folder names.
- [ ] TEST-036 Verify repo-central prompt keeps features under repository `.devcraft/features/<guid>`.
- [ ] TEST-037 Verify system-central prompt moves features under profile `.DevCraft/features/<guid>`.
- [ ] TEST-038 Verify prompt requires repository and profile feature history updates.
- [ ] TEST-039 Verify prompt states the current working directory is the active codebase.
- [ ] TEST-040 Verify prompt requires catalog updates so feature browsing works after upgrade.

### Phase 6: Beta 11 Release Prep

- Purpose: Prepare Beta 11 metadata and validation evidence without publishing unless separately requested.
- Expected Outcome: Code, tests, docs, and feature artifacts are ready for commit/tag/release.
- Entry Criteria: Upgrade behavior is implemented.
- Exit Criteria: Full local validation passes and results are recorded.

#### Application Development

- [ ] TASK-040 Bump DevCraft package version to `1.0.0-beta.11`.
- [ ] TASK-041 Update README install examples to `1.0.0-beta.11`.
- [ ] TASK-042 Update release selection tests to include beta 11.
- [ ] TASK-043 Record changed files and validation in `results.md`.

#### Tests And Validation

- [ ] TEST-041 Run focused upgrade tests.
- [ ] TEST-042 Run `dotnet test`.
- [ ] TEST-043 Run `dotnet build -c Release`.
- [ ] TEST-044 Run `tests/installer-release-selection.sh`.
- [ ] TEST-045 Run `dotnet list package --vulnerable --include-transitive`.
- [ ] TEST-046 Run `git diff --check`.
