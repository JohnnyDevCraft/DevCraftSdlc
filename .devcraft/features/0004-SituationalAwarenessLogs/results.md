# Feature Results: Situational Awareness Logs

## Feature Reference

- Feature ID: 0004
- Feature Name: Situational Awareness Logs
- Spec: [spec.md](./spec.md)
- Tasks: [tasks.md](./tasks.md)
- State: Implementation

## Implementation Summary

- Added profile-level situational awareness configuration defaults: disabled, `weeks`, `file`, and null connection.
- Added profile `situation` folder initialization and structured file storage using `people.json`, `log-entries.json`, and `summaries.json`.
- Added MongoDB-backed storage behind the same store abstraction, plus migration support between file and database stores.
- Added Configure DevCraft actions for situational awareness and desktop-agent setup instructions.
- Added the main Logging menu with Add Person, Add Log Entry, scale-aware compression commands, and no-data recovery.
- Added AI compression summary generation through supported terminal client scan operations and JSON parsing that accepts raw or fenced JSON.
- Added central situational-awareness prompt context injection for AI handoffs when enabled.
- Added beta 1 header polish: padded `Dev` before `Craft`, stripped build metadata from the visible version, and bumped the project version to `1.0.0-beta.1`.
- Added beta 2 installer release polish so one-command installs can resolve the current beta prerelease.
- Added beta 3 scan-failure diagnostics so Codex, Claude Code, and shared terminal-client failures report exit code, provider errors, stderr/stdout, parse failures, and bounded actionable hints.
- Added a post-beta-3 `select-agent` command and corrected terminal-client resolution so legacy soul values such as `Claude AI` resolve to Claude Code instead of falling back to Codex.
- Bumped the completed select-agent and terminal-client routing fixes to `1.0.0-beta.4` for release.
- Added forced DevCraft installation with `devcraft -force`, preserving other SDLC markers while installing only `.devcraft` artifacts.
- Removed root `AGENT.md` generation from DevCraft installation and preserved existing root `AGENT.md` and `AGENTS.md` files.
- Made existing `.devcraft` markers take precedence over other SDLC markers so DevCraft wins when multiple workflows are present.
- Updated DevCraft shared, seed profile, live profile, and project guidance to require TDD Red -> Green -> Refactor evidence.
- Bumped the forced-install and TDD guidance release to `1.0.0-beta.5` for Beta 5.
- Updated the GitHub release workflow so prerelease tags publish as prereleases.
- Added a conservative macOS installer hardening fix for launch-time `SIGKILL (Code Signature Invalid)` reports by replacing the installed Mach-O with a newly staged inode, ad-hoc signing/verifying the staged Mach-O on macOS, and then atomically renaming it into place.
- Changed situational-awareness AI handoff so prompts no longer embed people, log-entry, or summary payloads.
- Added file-mode handoff guidance that points the AI agent to actual profile situation files and explains how to read only active records.
- Added database-mode handoff guidance that tells the AI agent how to use local profile configuration to access MongoDB, read all people, and read only uncompressed log-entry and summary records without exposing the connection string in the prompt.
- Bumped the release to `1.0.0-beta.6` for Beta 6.
- Started post-beta-6 work for a main-menu `Situational Conversation` handoff that uses explicit installed-client selection and the existing storage-aware situational context guidance.
- Added profile-only conversational project/feature tracking support in `.DevCraft/features/projects.json` with canonical DevCraft feature statuses.
- Bumped the release to `1.0.0-beta.7` for Beta 7.
- Added Beta 8 people management under Logging with a Manage People submenu, sorted selectable people, in-place person editing, active/inactive toggles, inactive dates, and position fields.
- Added backwards-compatible legacy people loading with status normalization and default values for new fields.
- Bumped the release to `1.0.0-beta.8` for Beta 8.

## Files Changed

- `profile/configure.json` - Added beta 1 default `Situation*` settings.
- `profile/desktop-agent-instructions.txt` - Added seeded copy/paste instructions for desktop AI agents.
- `src/DevCraft.Cli/DevCraftProfileConfiguration.cs` - Added profile-level situation settings.
- `src/DevCraft.Cli/ProfileConfigurationReader.cs` - Normalizes defaults and supported situation values.
- `src/DevCraft.Cli/ProfileStructureInitializer.cs` - Creates `situation`, writes desktop-agent instructions, and preserves existing situation settings.
- `src/DevCraft.Cli/DevCraftMenuCommand.cs` - Added Logging, Configure Situational Awareness, desktop-agent instructions, and compression menu flow.
- `src/DevCraft.Cli/FeatureAiSessionLauncher.cs` - Appends active situation context to AI handoff prompts.
- `src/DevCraft.Cli/SpectreConsoleInteraction.cs` - Makes status messages visible so copy/paste setup text can be displayed.
- `src/DevCraft.Cli/CliLogoRenderer.cs` - Pads the logo prefix and strips version build metadata.
- `src/DevCraft.Cli/DevCraft.Cli.csproj` - Bumped version to `1.0.0-beta.1` and added MongoDB driver dependency, then updated it to `3.12.0` to avoid vulnerable transitive packages.
- `src/DevCraft.Cli/DevCraft.Cli.csproj` and `tests/DevCraft.Cli.Tests/CliLogoRendererTests.cs` - Bumped the Beta 2 release version and displayed version expectation to `1.0.0-beta.2`.
- `src/DevCraft.Cli/*Situation*.cs`, `src/DevCraft.Cli/JsonPayloadExtractor.cs`, `src/DevCraft.Cli/TerminalClientOutputExtractor.cs`, `src/DevCraft.Cli/MongoDockerInstructions.cs`, `src/DevCraft.Cli/CompressionSource.cs`, and `src/DevCraft.Cli/CompressionSummaryJson.cs` - Added situation models, storage, migration, prompt context, compression, and parser support.
- `src/DevCraft.Cli/ProjectScanJsonParser.cs` and `src/DevCraft.Cli/TerminalAiProjectScanner.cs` - Reused common JSON and terminal output extraction helpers.
- `tests/DevCraft.Cli.Tests/*` - Added focused tests for configuration defaults, file initialization, person/log writes, compression no-data behavior, fenced JSON parsing, handoff context inclusion/omission, database setup instructions, version display, and profile initialization updates.
- `.devcraft/AGENT.md`, `.devcraft/features/0004-SituationalAwarenessLogs/spec.md`, `.devcraft/features/0004-SituationalAwarenessLogs/tasks.md`, and this file - Updated DevCraft workflow records for the beta 1 implementation.
- `install.sh`, `install.ps1`, `README.md`, `tests/installer-release-selection.sh`, and `tests/InstallerReleaseSelection.Tests.ps1` - Resolved [ISSUE-001](./issues.md) so the normal installer selects the highest published SemVer release including beta prereleases, while preserving explicit version overrides.
- `src/DevCraft.Cli/TerminalAiProjectScanner.cs`, `src/DevCraft.Cli/TerminalClientOutputExtractor.cs`, `src/DevCraft.Cli/TerminalClientOutput.cs`, `src/DevCraft.Cli/TerminalProcessResult.cs`, `src/DevCraft.Cli/CodexLineKind.cs`, and `src/DevCraft.Cli/CodexLineResult.cs` - Captured terminal process exit data, extracted provider diagnostics from Codex NDJSON and Claude JSON output, preserved raw bounded output for invalid responses, and added recognized hints without letting recoverable warnings overshadow fatal provider errors.
- `src/DevCraft.Cli/SupportedTerminalClientCatalog.cs` - Added `--skip-git-repo-check` for Codex scans and JSON output mode for Claude Code scans.
- `src/DevCraft.Cli/AssemblyInfo.cs` - Exposed internal scanner diagnostic helpers to the focused test assembly.
- `src/DevCraft.Cli/DevCraft.Cli.csproj` and `tests/DevCraft.Cli.Tests/CliLogoRendererTests.cs` - Bumped the Beta 3 release version and displayed version expectation to `1.0.0-beta.3`.
- `tests/DevCraft.Cli.Tests/TerminalAiProjectScannerTests.cs`, `tests/DevCraft.Cli.Tests/TerminalClientOutputExtractorTests.cs`, and `tests/DevCraft.Cli.Tests/SupportedTerminalClientCatalogTests.cs` - Added regression coverage for missing client startup text, Codex unsupported-model NDJSON, recoverable warning demotion, Claude JSON result/error output, invalid response diagnostics, and terminal scan argument expectations.
- `src/DevCraft.Cli/AgentSelectionCommand.cs`, `src/DevCraft.Cli/SoulDefaultAgentStore.cs`, `src/DevCraft.Cli/SupportedTerminalClientResolver.cs`, and `src/DevCraft.Cli/SupportedTerminalClientProfileNormalizer.cs` - Added selectable default-agent persistence, shared client resolution, and profile client normalization that preserves custom executable paths while refreshing built-in arguments.
- `src/DevCraft.Cli/DevCraftCli.cs`, `src/DevCraft.Cli/StartupFlow.cs`, `src/DevCraft.Cli/TerminalAiProjectScanner.cs`, and `src/DevCraft.Cli/IAiProjectScanner.cs` - Wired `devcraft select-agent` before normal startup, passed the profile directory into scans, and switched scans to the resolved configured client operation instead of hardcoded client branches.
- `profile/configure.json` - Updated seeded Codex and Claude Code scan arguments to include the post-beta-3 scan fixes.
- `tests/DevCraft.Cli.Tests/AgentSelectionCommandTests.cs`, `tests/DevCraft.Cli.Tests/SupportedTerminalClientCatalogTests.cs`, `tests/DevCraft.Cli.Tests/TerminalAiProjectScannerTests.cs`, and `tests/DevCraft.Cli.Tests/FakeAiProjectScanner.cs` - Added tests for selection persistence, normal startup scan reuse, legacy `Claude AI` resolution, custom executable preservation, and Codex-output mismatch diagnostics.
- `src/DevCraft.Cli/DevCraft.Cli.csproj` and `tests/DevCraft.Cli.Tests/CliLogoRendererTests.cs` - Bumped the Beta 4 release version and displayed version expectation to `1.0.0-beta.4`.
- `src/DevCraft.Cli/DevCraftCli.cs` - Added `devcraft -force` argument handling and rejected unsupported extra arguments.
- `src/DevCraft.Cli/StartupFlow.cs` - Added force-install startup flow and made existing DevCraft markers preferred over other SDLC markers.
- `src/DevCraft.Cli/DevCraftInstaller.cs` - Stopped creating root `AGENT.md` while preserving `.devcraft/AGENT.md` and all other control files.
- `tests/DevCraft.Cli.Tests/DevCraftInstallerTests.cs` - Added coverage proving installation does not create root `AGENT.md` and preserves existing root agent files.
- `tests/DevCraft.Cli.Tests/StartupFlowTests.cs` - Added coverage for forced installation, force bypassing scans, and DevCraft precedence over other SDLC markers.
- `profile/DevCraft.md`, `profile/templates/AGENT.template.md`, `profile/templates/tasks.template.md`, and `profile/templates/results.template.md` - Updated seeded profile guidance and templates for `.devcraft`-only install behavior and TDD evidence.
- `.devcraft/AGENT.md` - Recorded the project-level TDD and root-agent preservation working agreements.
- `/Users/john/codex-setup/modes/DevCraft.md` and `/Users/john/codex-setup/modes/DevCraft/templates/*` - Updated the shared source of truth for `.devcraft`-only install behavior and TDD evidence.
- `/Users/john/.DevCraft/DevCraft.md` and `/Users/john/.DevCraft/templates/*` - Updated the live installed DevCraft operating rules and templates for `.devcraft`-only install behavior and TDD evidence.
- `src/DevCraft.Cli/DevCraft.Cli.csproj` and `tests/DevCraft.Cli.Tests/CliLogoRendererTests.cs` - Bumped the Beta 5 release version and displayed version expectation to `1.0.0-beta.5`.
- `README.md`, `tests/installer-release-selection.sh`, and `tests/InstallerReleaseSelection.Tests.ps1` - Updated installer documentation and release-selection tests for `1.0.0-beta.5`.
- `.github/workflows/release.yml` - Marked GitHub releases as prerelease when the pushed tag contains a prerelease suffix.
- `install.sh` - Changed Unix install to copy the binary to a temporary file, ad-hoc sign and verify Mach-O binaries on macOS, then atomically rename the staged binary over the previous install path.
- `install.ps1` - Changed Windows install to copy through a temporary file before replacing the installed executable.
- `tests/installer-release-selection.sh` - Added coverage proving installer replacement creates a new installed `devcraft` inode.
- `src/DevCraft.Cli/SituationPromptContextBuilder.cs` - Replaced serialized situational payload injection with file-mode and database-mode read instructions.
- `tests/DevCraft.Cli.Tests/SituationPromptContextBuilderTests.cs` - Added coverage for no embedded record payloads, disabled omission, file-mode guidance, database-mode guidance, connection-string omission, and no generated database snapshot.
- `profile/DevCraft.md` - Documented situational-awareness AI handoff behavior for file and database storage modes.
- `src/DevCraft.Cli/DevCraft.Cli.csproj` and `tests/DevCraft.Cli.Tests/CliLogoRendererTests.cs` - Bumped the Beta 6 release version and displayed version expectation to `1.0.0-beta.6`.
- `README.md`, `tests/installer-release-selection.sh`, and `tests/InstallerReleaseSelection.Tests.ps1` - Updated installer documentation and release-selection tests for `1.0.0-beta.6`.
- `.devcraft/features/0004-SituationalAwarenessLogs/spec.md`, `.devcraft/features/0004-SituationalAwarenessLogs/tasks.md`, and this file - Recorded the authorized `Situational Conversation` menu requirement and Red-Green-Refactor validation tasks.
- `src/DevCraft.Cli/DevCraftMenuCommand.cs` - Added the exact `Situational Conversation` main-menu item, disabled-state notice, and explicit selected-client launch through the common DevCraft AI-session launcher.
- `tests/DevCraft.Cli.Tests/DevCraftMenuCommandTests.cs` - Added focused red tests for main-menu visibility, enabled planning handoff, and disabled-state notice.
- `src/DevCraft.Cli/SystemCentralProjectsStore.cs`, `src/DevCraft.Cli/SystemCentralProject.cs`, and `src/DevCraft.Cli/DevCraftFeature.cs` - Added profile `projects.json` read/write support for `id`, `repo-location`, `repo-name`, `feature-name`, `description`, `work-item-id`, and `status` while preserving legacy System Central reads and existing GUID folder mapping.
- `tests/DevCraft.Cli.Tests/FeatureCommandTests.cs` - Added profile index tests for the conversational tracking shape, default canonical status, and legacy index compatibility.
- `src/DevCraft.Cli/DevCraft.Cli.csproj` and `tests/DevCraft.Cli.Tests/CliLogoRendererTests.cs` - Bumped the Beta 7 release version and displayed version expectation to `1.0.0-beta.7`.
- `README.md`, `tests/installer-release-selection.sh`, and `tests/InstallerReleaseSelection.Tests.ps1` - Updated installer documentation and release-selection tests for `1.0.0-beta.7`.
- `src/DevCraft.Cli/SituationPerson.cs` - Added job title, assigned team, organization, and inactive date fields while preserving defaults for legacy records.
- `src/DevCraft.Cli/ISituationStore.cs`, `src/DevCraft.Cli/FileSituationStore.cs`, and `src/DevCraft.Cli/MongoSituationStore.cs` - Added person upsert support and normalized legacy active/inactive status values.
- `src/DevCraft.Cli/DevCraftMenuCommand.cs` - Changed Logging from direct Add Person to Manage People, added sorted list/edit flows, active/inactive status changes, and expanded Add Person prompts.
- `src/DevCraft.Cli/SituationPromptContextBuilder.cs` - Updated file and database guidance with the new people fields.
- `tests/DevCraft.Cli.Tests/DevCraftMenuCommandTests.cs`, `tests/DevCraft.Cli.Tests/SituationStorageTests.cs`, and `tests/DevCraft.Cli.Tests/SituationPromptContextBuilderTests.cs` - Added focused Beta 8 coverage for Manage People, sorting, edit/upsert, inactive dates, legacy records, and prompt guidance.
- `src/DevCraft.Cli/DevCraft.Cli.csproj` and `tests/DevCraft.Cli.Tests/CliLogoRendererTests.cs` - Bumped the Beta 8 release version and displayed version expectation to `1.0.0-beta.8`.
- `README.md`, `tests/installer-release-selection.sh`, and `tests/InstallerReleaseSelection.Tests.ps1` - Updated installer documentation and release-selection tests for `1.0.0-beta.8`.
- `src/DevCraft.Cli/TextEditOptions.cs`, `src/DevCraft.Cli/TextEditResult.cs`, and `src/DevCraft.Cli/TerminalGuiTextEditor.cs` - Added a reusable Terminal.Gui multiline editor with save/cancel commands, markdown-aware editor support, line numbers, word wrap, paste, undo/redo, and return/tab-friendly editing.
- `src/DevCraft.Cli/FieldEditOptions.cs`, `src/DevCraft.Cli/FieldEditResult.cs`, and `src/DevCraft.Cli/TerminalGuiFieldEditor.cs` - Added a reusable Terminal.Gui field form for structured single-line edits.
- `src/DevCraft.Cli/IConsoleInteraction.cs`, `src/DevCraft.Cli/SpectreConsoleInteraction.cs`, and `tests/DevCraft.Cli.Tests/FakeConsoleInteraction.cs` - Routed field and large-text editing through reusable console-interaction adapters.
- `src/DevCraft.Cli/ISituationStore.cs`, `src/DevCraft.Cli/FileSituationStore.cs`, and `src/DevCraft.Cli/MongoSituationStore.cs` - Added log-entry and summary upsert operations that preserve row identity, compression status, and summary type.
- `src/DevCraft.Cli/DevCraftMenuCommand.cs` - Added Manage Logs, uncompressed log-entry and summary listing/editing, new multiline log entry creation, Terminal.Gui-backed people forms, and Configure DevCraft supported-client editing.
- `src/DevCraft.Cli/StartupFlow.cs` - Routed soul setup, response style, custom project descriptions, and AI-suggested project description overrides through the multiline editor.
- `src/DevCraft.Cli/SupportedTerminalClientProfileNormalizer.cs` - Preserved operator-authored supported-client names, descriptions, binaries, and argument arrays during startup normalization.
- `tests/DevCraft.Cli.Tests/DevCraftMenuCommandTests.cs`, `tests/DevCraft.Cli.Tests/SituationStorageTests.cs`, `tests/DevCraft.Cli.Tests/StartupFlowTests.cs`, and `tests/DevCraft.Cli.Tests/SupportedTerminalClientCatalogTests.cs` - Added and updated Beta 9 coverage for Manage Logs, summary/log upserts, supported-client editing, large-text routing, and operator-authored client preservation.
- `src/DevCraft.Cli/DevCraft.Cli.csproj` and `tests/DevCraft.Cli.Tests/CliLogoRendererTests.cs` - Bumped the Beta 9 release version and displayed version expectation to `1.0.0-beta.9`.
- `README.md`, `tests/installer-release-selection.sh`, and `tests/InstallerReleaseSelection.Tests.ps1` - Updated installer documentation and release-selection tests for `1.0.0-beta.9`.
- `src/DevCraft.Cli/DevCraftMenuCommand.cs` - Moved existing project actions under a new `Project Management` submenu and retained the existing `Project theme` action/label because the codebase has a theme workflow, not a team workflow.
- `install.sh` and `install.ps1` - Stopped package profile copies from overwriting an existing profile `configure.json`, preserving situational-awareness settings during routine upgrades.
- `src/DevCraft.Cli/CatalogMergeCommand.cs` - Preserved target profile situational-awareness settings when merging external catalogs.
- `tests/DevCraft.Cli.Tests/DevCraftMenuCommandTests.cs`, `tests/DevCraft.Cli.Tests/CatalogMergeCommandTests.cs`, `tests/DevCraft.Cli.Tests/ProfileStructureInitializerTests.cs`, `tests/installer-release-selection.sh`, and `tests/InstallerReleaseSelection.Tests.ps1` - Added regressions for Project Management menu shape/return behavior and situational-awareness settings preservation across initialization, merge, and installer copy paths.
- `src/DevCraft.Cli/DevCraftProfileConfiguration.cs`, `src/DevCraft.Cli/ProfileConfigurationReader.cs`, and `profile/configure.json` - Added explicit profile configuration `SchemaVersion` support with legacy migration to the current schema and future-schema rejection.
- `tests/DevCraft.Cli.Tests/ProfileConfigurationReaderTests.cs` - Added coverage for legacy migration preserving situational-awareness settings, current-schema idempotence, and unknown future schema rejection.

## TDD Evidence

### Force Install And Root Agent Preservation

- Red: `dotnet test --filter "FullyQualifiedName~DevCraftInstallerTests|FullyQualifiedName~StartupFlowTests"` failed to compile because `StartupFlow.Run` had no `forceInstall` parameter after adding the first force-install tests.
- Red: `dotnet test --filter "FullyQualifiedName~StartupFlowTests"` failed `RunInstallsDevCraftWhenForcedInCodeFolderWithoutScanning` because force did not install in a code folder without SDLC markers.
- Green: `dotnet test --filter "FullyQualifiedName~DevCraftInstallerTests|FullyQualifiedName~StartupFlowTests"` passed with 13 focused tests after wiring `devcraft -force`, removing root `AGENT.md` generation, and making force bypass scans.
- Refactor: Shared/profile/live rules and templates were updated after the focused green run to make TDD evidence and root-agent preservation part of the reusable DevCraft workflow.

### macOS Installer Code-Signing Hardening

- Red: `tests/installer-release-selection.sh` failed with `FAIL: install should replace devcraft with a new inode`, proving the previous Unix installer overwrote the existing executable in place.
- Green: `tests/installer-release-selection.sh` passed after staging the binary to a temporary path, macOS ad-hoc signing/verifying the staged Mach-O, and atomically renaming it into place.
- Refactor: The Windows installer was aligned to the same staged-copy replacement pattern even though the observed failure is macOS-specific.

### Situational Awareness File And Database Handoff

- Red: `dotnet test --filter "FullyQualifiedName~SituationPromptContextBuilderTests"` failed after adding tests for file/database read guidance because the builder still materialized a handoff snapshot and accepted an injected store.
- Green: `dotnet test --filter "FullyQualifiedName~SituationPromptContextBuilderTests"` passed after file mode pointed at `people.json`, `log-entries.json`, and `summaries.json`, and database mode pointed at MongoDB collections and `IsCompressed=false` filters without embedding payloads or credentials.
- Refactor: The unused handoff snapshot writer and fake situation store were removed after the user clarified database mode should not export a file snapshot.

### Situational Conversation Menu

- Red: `dotnet test --filter "FullyQualifiedName~DevCraftMenuCommandTests"` failed after adding expectations for `Situational Conversation`: the option was missing from the main menu, no planning handoff launched, and the disabled-state notice was absent.
- Green: `dotnet test --filter "FullyQualifiedName~DevCraftMenuCommandTests"` passed with 19 focused menu tests after adding the main-menu option, disabled-state notice, and common-launcher handoff for a selected installed AI client.
- Refactor: The handoff instruction was tightened so the conversation starts by asking what the user wants to accomplish today without framing the flow as repository discovery or feature work.

### Profile Projects Conversation Tracking

- Red: `dotnet test --filter "FullyQualifiedName~FeatureCommandTests|FullyQualifiedName~DevCraftMenuCommandTests|FullyQualifiedName~DevCraftInstallerTests"` failed to compile after adding focused expectations because profile project tracking had no `RepositoryName` or feature `Status` surface yet.
- Green: `dotnet test --filter "FullyQualifiedName~FeatureCommandTests|FullyQualifiedName~DevCraftMenuCommandTests|FullyQualifiedName~DevCraftInstallerTests"` passed with 27 focused tests after adding profile-only `projects.json` schema output, legacy read support, canonical status defaults, and situational conversation tracking guidance.
- Refactor: The implementation was narrowed from the initial broad schema direction to profile `.DevCraft/features/projects.json` only, preserving repository `.devcraft/configure.json` shape and current feature command semantics.

### People Management

- Red: `dotnet test --filter "FullyQualifiedName~SituationStorageTests|FullyQualifiedName~DevCraftMenuCommandTests|FullyQualifiedName~SituationPromptContextBuilderTests"` failed to compile because `SituationPerson` lacked Beta 8 people fields, the storage abstraction lacked person upsert support, and the menu still exposed only direct Add Person behavior.
- Green: `dotnet test --filter "FullyQualifiedName~SituationStorageTests|FullyQualifiedName~DevCraftMenuCommandTests|FullyQualifiedName~SituationPromptContextBuilderTests"` passed with 33 focused tests after adding Manage People, sorted selectable people, edit flows, status toggles, inactive dates, storage upserts, legacy normalization, and updated handoff guidance.
- Refactor: File and Mongo stores keep status normalization at the provider boundary, and person edits use the same `RowId` to avoid duplicate rows across storage modes.

### Terminal.Gui Editing And Manage Logs

- Red: `dotnet test --filter "FullyQualifiedName~SituationStorageTests|FullyQualifiedName~DevCraftMenuCommandTests|FullyQualifiedName~StartupFlowTests|FullyQualifiedName~SupportedTerminalClientCatalogTests"` initially failed after adding Beta 9 expectations because log/summary upserts, Manage Logs, supported-client editing, and multiline editor routing were not present.
- Green: The same focused test command passed with 48 focused tests after adding reusable Terminal.Gui editors, Manage Logs, log-entry and summary edit flows, supported-client configuration editing, and startup large-text routing.
- Refactor: Supported-client profile normalization now fills only missing defaults and preserves explicit operator-authored values so startup cannot wipe manual client changes.

### Project Management And SA-Preserving Upgrades

- Red: Focused menu tests proved project actions were still top-level main-menu choices instead of grouped under `Project Management`.
- Red: Installer and merge inspection found profile `configure.json` overwrite/reset paths: package profile copies could overwrite existing situational-awareness settings, and catalog merge rebuilt configuration without carrying the target situation settings forward.
- Red: Profile configuration had no explicit schema-version migration path, so legacy/current/future-schema behavior was implicit rather than governed.
- Green: `dotnet test --filter "FullyQualifiedName~DevCraftMenuCommandTests|FullyQualifiedName~CatalogMergeCommandTests|FullyQualifiedName~ProfileStructureInitializerTests|FullyQualifiedName~SituationStorageTests"` passed with 45 focused tests after adding the submenu and preservation fixes.
- Green: `dotnet test --filter "FullyQualifiedName~ProfileConfigurationReaderTests|FullyQualifiedName~ProfileStructureInitializerTests|FullyQualifiedName~CatalogMergeCommandTests|FullyQualifiedName~DevCraftMenuCommandTests"` passed with 38 focused tests after adding explicit schema migration behavior.
- Green: `tests/installer-release-selection.sh` passed after adding a regression that preserves enabled MongoDB sprint settings and a custom connection string during package install.
- Refactor: The submenu keeps the exact existing `Project theme` action because the current workflow is theme-oriented; no new Project Team workflow was added. Future profile configuration schemas are rejected before profile initialization or merge can destructively rewrite them.

## Validation

- `dotnet list package --vulnerable --include-transitive` passed with no vulnerable packages reported by the configured sources after updating `MongoDB.Driver` to `3.12.0`.
- `dotnet build` passed with 0 warnings and 0 errors.
- `dotnet test` passed: 85 tests.
- `dotnet publish src/DevCraft.Cli/DevCraft.Cli.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o artifacts/publish/osx-arm64` passed.
- Ran the published macOS Apple Silicon binary and visually verified the header shows `Version 1.0.0-beta.1` without build metadata and with the `Craft` segment padded to the right of `Dev`.
- `tests/installer-release-selection.sh` passed.
- Live read-only shell release resolution selected `v1.0.0-beta.1` and `DevCraft-osx-arm64.tar.gz`.
- Live read-only shell release resolution with `DEVCRAFT_VERSION=1.0.0-alpha.12` selected `v1.0.0-alpha.12`.
- Beta 2 release validation will verify the normal live installer resolution selects `v1.0.0-beta.2` after the release is published.
- `dotnet publish src/DevCraft.Cli/DevCraft.Cli.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o artifacts/publish/osx-arm64` passed again after the Beta 2 version bump.
- `dotnet test` passed after the Beta 3 scan diagnostics: 93 tests.
- `dotnet build -c Release` passed with 0 warnings and 0 errors after the Beta 3 scan diagnostics.
- `tests/installer-release-selection.sh` passed after the Beta 3 version bump.
- `dotnet publish src/DevCraft.Cli/DevCraft.Cli.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o artifacts/publish/osx-arm64` passed after the Beta 3 version bump.
- `dotnet test` passed after the `select-agent` and scanner-resolution fixes: 101 tests.
- `dotnet list package --vulnerable --include-transitive` passed with no vulnerable packages reported by the configured sources after the `select-agent` and scanner-resolution fixes.
- `dotnet build -c Release` passed with 0 warnings and 0 errors after the `select-agent` and scanner-resolution fixes.
- `dotnet publish src/DevCraft.Cli/DevCraft.Cli.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o artifacts/publish/osx-arm64` passed after the `select-agent` and scanner-resolution fixes.
- `dotnet list package --vulnerable --include-transitive` passed with no vulnerable packages reported by the configured sources after the Beta 4 version bump.
- `dotnet test` passed after the Beta 4 version bump: 101 tests.
- `dotnet build -c Release` passed with 0 warnings and 0 errors after the Beta 4 version bump.
- `tests/installer-release-selection.sh` passed after the Beta 4 version bump.
- `dotnet publish src/DevCraft.Cli/DevCraft.Cli.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o artifacts/publish/osx-arm64` passed after the Beta 4 version bump.
- `dotnet test --filter "FullyQualifiedName~DevCraftInstallerTests|FullyQualifiedName~StartupFlowTests"` passed after adding `devcraft -force` and root-agent preservation coverage: 13 focused tests.
- `dotnet test` passed after forced-install and TDD guidance updates: 105 tests.
- `dotnet build -c Release` passed with 0 warnings and 0 errors after forced-install and TDD guidance updates.
- `dotnet list package --vulnerable --include-transitive` passed with no vulnerable packages reported by the configured sources after forced-install and TDD guidance updates.
- `dotnet publish src/DevCraft.Cli/DevCraft.Cli.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o artifacts/publish/osx-arm64` passed after forced-install and TDD guidance updates.
- `dotnet test` passed after the Beta 5 version bump and release workflow update: 105 tests.
- `tests/installer-release-selection.sh` passed after the Beta 5 release-selection updates.
- `dotnet build -c Release` passed with 0 warnings and 0 errors after the Beta 5 version bump.
- `dotnet list package --vulnerable --include-transitive` passed with no vulnerable packages reported by the configured sources after the Beta 5 version bump.
- `dotnet publish src/DevCraft.Cli/DevCraft.Cli.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o artifacts/publish/osx-arm64` passed after the Beta 5 version bump.
- `codesign --verify --deep --strict --verbose=4 artifacts/publish/osx-arm64/DevCraft.Cli` passed for the local Beta 5 macOS arm64 binary; `file` and `lipo -archs` reported a thin arm64 Mach-O.
- Isolated upgrade validation passed by installing published `v1.0.0-beta.4` into a temp `DEVCRAFT_HOME`, overwriting it with the local Beta 5 package, verifying the installed binary signature, and confirming a seeded isolated startup exited 0 with `Version 1.0.0-beta.5` and no unhandled exception.
- Isolated patched-installer validation passed by installing published `v1.0.0-beta.5`, reinstalling with the patched local package, proving the installed binary inode changed, verifying the installed Mach-O code signature, and confirming seeded startup displayed `Version 1.0.0-beta.5`.
- `dotnet test` passed after installer hardening: 105 tests.
- `dotnet build -c Release` passed with 0 warnings and 0 errors after installer hardening.
- `dotnet list package --vulnerable --include-transitive` passed with no vulnerable packages reported by the configured sources after installer hardening.
- `dotnet publish src/DevCraft.Cli/DevCraft.Cli.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o artifacts/publish/osx-arm64` passed after installer hardening.
- `dotnet test --filter "FullyQualifiedName~SituationPromptContextBuilderTests"` passed after situational-awareness handoff guidance updates: 3 focused tests.
- `tests/installer-release-selection.sh` passed after adding platform-binary validation and Beta 6 release-selection coverage.
- `dotnet test` passed after Beta 6 handoff, installer, and version updates: 106 tests.
- `tests/installer-release-selection.sh` passed after Beta 6 handoff, installer, and version updates.
- `dotnet build -c Release` passed with 0 warnings and 0 errors after Beta 6 handoff, installer, and version updates.
- `dotnet list package --vulnerable --include-transitive` passed with no vulnerable packages reported by the configured sources after Beta 6 updates.
- `dotnet publish src/DevCraft.Cli/DevCraft.Cli.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o artifacts/publish/osx-arm64` passed after Beta 6 updates.
- Isolated Beta 5 to local Beta 6 upgrade validation passed: the installed macOS arm64 binary inode changed, `codesign --verify --deep --strict --verbose=4` passed, and seeded startup displayed `Version 1.0.0-beta.6`.
- `dotnet test --filter "FullyQualifiedName~DevCraftMenuCommandTests"` passed after the `Situational Conversation` menu addition: 19 focused menu tests.
- `dotnet test` passed after the `Situational Conversation` menu addition: 109 tests.
- `dotnet build -c Release` passed with 0 warnings and 0 errors after the `Situational Conversation` menu addition.
- `dotnet list package --vulnerable --include-transitive` passed with no vulnerable packages reported by the configured sources after the `Situational Conversation` menu addition.
- `dotnet publish src/DevCraft.Cli/DevCraft.Cli.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o artifacts/publish/osx-arm64` passed after the `Situational Conversation` menu addition.
- `file artifacts/publish/osx-arm64/DevCraft.Cli` reported a Mach-O 64-bit arm64 executable, and `codesign --verify --deep --strict --verbose=4 artifacts/publish/osx-arm64/DevCraft.Cli` passed.
- `dotnet test --filter "FullyQualifiedName~FeatureCommandTests|FullyQualifiedName~DevCraftMenuCommandTests|FullyQualifiedName~DevCraftInstallerTests"` passed after the profile projects tracking update: 27 focused tests.
- `dotnet test` passed after Beta 7 updates: 110 tests.
- `dotnet build -c Release` passed with 0 warnings and 0 errors after Beta 7 updates.
- `dotnet list package --vulnerable --include-transitive` passed with no vulnerable packages reported by the configured sources after Beta 7 updates.
- `tests/installer-release-selection.sh` passed after Beta 7 release-selection updates.
- `dotnet publish src/DevCraft.Cli/DevCraft.Cli.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o artifacts/publish/osx-arm64` passed after Beta 7 updates.
- `file artifacts/publish/osx-arm64/DevCraft.Cli` reported a Mach-O 64-bit arm64 executable, and `codesign --verify --deep --strict --verbose=4 artifacts/publish/osx-arm64/DevCraft.Cli` passed after Beta 7 updates.
- Local isolated package installation passed in a temporary `DEVCRAFT_HOME`: the installed binary inode changed, `codesign --verify --deep --strict --verbose=4` passed, and isolated startup displayed `Version 1.0.0-beta.7`.
- PowerShell Core was not installed on the validation host, so `tests/InstallerReleaseSelection.Tests.ps1` was updated for Beta 7 but not executed locally.
- `dotnet test --filter "FullyQualifiedName~SituationStorageTests|FullyQualifiedName~DevCraftMenuCommandTests|FullyQualifiedName~SituationPromptContextBuilderTests"` passed after the Beta 8 people management changes: 33 focused tests.
- `dotnet test` passed after Beta 8 updates: 115 tests.
- `dotnet build -c Release` passed with 0 warnings and 0 errors after Beta 8 updates.
- `dotnet list package --vulnerable --include-transitive` passed with no vulnerable packages reported by the configured sources after Beta 8 updates.
- `tests/installer-release-selection.sh` passed after Beta 8 release-selection updates.
- `dotnet publish src/DevCraft.Cli/DevCraft.Cli.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o artifacts/publish/osx-arm64` passed after Beta 8 updates.
- `file artifacts/publish/osx-arm64/DevCraft.Cli` reported a Mach-O 64-bit arm64 executable, and `codesign --verify --deep --strict --verbose=4 artifacts/publish/osx-arm64/DevCraft.Cli` passed after Beta 8 updates.
- Local published startup displayed `Version 1.0.0-beta.8`.
- PowerShell Core was not installed on the validation host, so `tests/InstallerReleaseSelection.Tests.ps1` was updated for Beta 8 but not executed locally.
- `dotnet test --filter "FullyQualifiedName~SituationStorageTests|FullyQualifiedName~DevCraftMenuCommandTests|FullyQualifiedName~StartupFlowTests|FullyQualifiedName~SupportedTerminalClientCatalogTests"` passed after Beta 9 editor and Manage Logs work: 48 focused tests.
- `dotnet test` passed after Beta 9 updates: 120 tests.
- `dotnet build -c Release` passed with 2 xUnit analyzer warnings and 0 errors after Beta 9 updates.
- `dotnet list package --vulnerable --include-transitive` passed with no vulnerable packages reported by the configured sources after Beta 9 updates.
- `tests/installer-release-selection.sh` passed after Beta 9 release-selection updates.
- PowerShell Core was not installed on the validation host, so `tests/InstallerReleaseSelection.Tests.ps1` was updated for Beta 9 but not executed locally.
- `dotnet publish src/DevCraft.Cli/DevCraft.Cli.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -o artifacts/beta9-osx-arm64` passed after Beta 9 updates.
- `file artifacts/beta9-osx-arm64/DevCraft.Cli` reported a Mach-O 64-bit arm64 executable, and `codesign -dv artifacts/beta9-osx-arm64/DevCraft.Cli` reported an embedded ad hoc signature.
- Isolated profile smoke with `HOME=/tmp/... artifacts/beta9-osx-arm64/DevCraft.Cli list` passed and listed seeded profile catalogs plus supported clients without touching the live installed binary.
- Interactive PTY validation with `TERM=xterm-256color` passed for Manage Logs: keyboard navigation opened the Terminal.Gui editor, multiline text plus a tab were saved with Ctrl+S, List Log Entries displayed only the uncompressed record, reopening edit showed existing content, and Esc cancel returned without modifying the stored JSON.
- Interactive PTY validation also passed for the Terminal.Gui field form path: Add Person accepted Tab navigation across fields, Ctrl+S saved the structured form, the relationship multiline editor saved with Ctrl+S, ACTIVE status was selected, and `people.json` contained the expected person fields and multiline relationship.
- Terminal.Gui.Editor package documentation lists Markdown as a built-in highlighting definition, and the reusable editor sets `HighlightingManager.Instance.GetDefinitionByExtension(".md")` when Markdown highlighting is preferred.
- `dotnet test --filter "FullyQualifiedName~DevCraftMenuCommandTests|FullyQualifiedName~CatalogMergeCommandTests|FullyQualifiedName~ProfileStructureInitializerTests|FullyQualifiedName~SituationStorageTests"` passed after Project Management and SA-preservation work: 45 focused tests.
- `dotnet test` passed after Project Management and SA-preservation work: 124 tests.
- `dotnet build -c Release` passed with 2 xUnit analyzer warnings and 0 errors after Project Management and SA-preservation work.
- `dotnet list package --vulnerable --include-transitive` passed with no vulnerable packages reported by the configured sources after Project Management and SA-preservation work.
- `tests/installer-release-selection.sh` passed after the install preservation regression.
- PowerShell Core was not installed on the validation host, so `tests/InstallerReleaseSelection.Tests.ps1` was updated with install preservation coverage but not executed locally.
- Local redeploy to `/Users/john/.DevCraft/devcraft` used a staged signed arm64 Mach-O and atomic rename. The installed binary displayed `Version 1.0.0-beta.9`, `devcraft list` passed, and SHA-256 checksums for `/Users/john/.DevCraft/configure.json` and `/Users/john/.DevCraft/soul.md` matched before and after deployment.
- `dotnet test` passed after schema migration work: 127 tests.
- `dotnet build -c Release` passed after schema migration work with 2 xUnit analyzer warnings and 0 errors.
- `tests/installer-release-selection.sh` passed after schema migration work.
- `dotnet list package --vulnerable --include-transitive` passed with no vulnerable packages reported after schema migration work.
- PowerShell Core was still not installed on the validation host, so `tests/InstallerReleaseSelection.Tests.ps1` remained updated but not executed locally.
- Local redeploy to `/Users/john/.DevCraft/devcraft` used a staged signed arm64 Mach-O and atomic rename. The installed binary displayed `Version 1.0.0-beta.9`, `devcraft list` passed, and SHA-256 checksums for `/Users/john/.DevCraft/configure.json` and `/Users/john/.DevCraft/soul.md` again matched before and after deployment.

### Beta 10 Header Context And Editor Chrome

- Red coverage target: Existing header coverage only asserted the plain version string, and no automated coverage existed for cached directory/branch/divergence context, home-path display, or disabled Terminal.Gui editor gutters.
- Green: The focused header/editor test command passed with 9 tests after adding process-level header context caching, git branch/divergence lookup, `~` path formatting, and `GutterOptions.None` for long text editing.
- Refactor: `TerminalGuiTextEditorWindow` and `TerminalGuiFieldEditorWindow` were moved into separate files to satisfy the one-type-per-file rule while both Terminal.Gui editor surfaces gained the shared header above their content.

#### Files Changed

- `src/DevCraft.Cli/CliHeaderContext.cs` - Added an immutable header context for the current directory, Git branch, and upstream divergence.
- `src/DevCraft.Cli/CliHeaderContextCache.cs` - Added process memory for the startup header context so render calls reuse one captured snapshot.
- `src/DevCraft.Cli/CliHeaderContextProvider.cs` - Added startup Git command execution for branch and ahead/behind state, including detached and no-upstream fallback behavior.
- `src/DevCraft.Cli/DisplayPathFormatter.cs` - Added home-directory shortening so visible paths render like `~/Source/repos/...`.
- `src/DevCraft.Cli/CliLogoRenderer.cs` - Updated the visible version line to include the cached directory, branch, and divergence.
- `src/DevCraft.Cli/DevCraftCli.cs` - Initialized the header context cache as the first startup step.
- `src/DevCraft.Cli/TerminalGuiHeader.cs` - Added a shared Terminal.Gui header renderer that reuses the DevCraft logo text.
- `src/DevCraft.Cli/TerminalGuiTextEditor.cs` - Placed long text editing under the header and disabled line-number gutters.
- `src/DevCraft.Cli/TerminalGuiFieldEditor.cs` - Placed field editing under the same header so Terminal.Gui surfaces preserve the DevCraft chrome.
- `src/DevCraft.Cli/TerminalGuiTextEditorWindow.cs` and `src/DevCraft.Cli/TerminalGuiFieldEditorWindow.cs` - Split existing window support types into their own files.
- `src/DevCraft.Cli/DevCraft.Cli.csproj`, `README.md`, `tests/installer-release-selection.sh`, and `tests/InstallerReleaseSelection.Tests.ps1` - Bumped local release metadata and installer selection expectations to `1.0.0-beta.10`.
- `tests/DevCraft.Cli.Tests/CliLogoRendererTests.cs`, `tests/DevCraft.Cli.Tests/CliHeaderContextProviderTests.cs`, `tests/DevCraft.Cli.Tests/DisplayPathFormatterTests.cs`, and `tests/DevCraft.Cli.Tests/TerminalGuiTextEditorTests.cs` - Added regression coverage for the Beta 10 header and editor changes.

#### Validation

- `dotnet test --filter "FullyQualifiedName~CliHeaderContextProviderTests|FullyQualifiedName~CliLogoRendererTests|FullyQualifiedName~DisplayPathFormatterTests|FullyQualifiedName~TerminalGuiTextEditorTests"` passed: 10 focused tests.
- `dotnet test` passed: 132 tests.
- `tests/installer-release-selection.sh` passed.
- `dotnet build -c Release` passed with the existing 2 xUnit analyzer warnings and 0 errors.
- `dotnet list package --vulnerable --include-transitive` passed with no vulnerable packages reported.
- `git diff --check` passed.

## Notes

- Earlier `MongoDB.Driver 3.5.0` restore output reported vulnerable transitive `SharpCompress` and `Snappier` packages. Updating to `MongoDB.Driver 3.12.0` resolved those warnings in `dotnet list package --vulnerable --include-transitive`.
- Database migration code is implemented, but automated tests avoid requiring a live MongoDB instance. The configuration flow displays Docker setup instructions rather than starting Docker.
- PowerShell Core was not installed on the validation host, so `tests/InstallerReleaseSelection.Tests.ps1` was added but not executed locally.
- The Beta 4 instant-kill report was not reproduced locally during isolated Beta 4 to Beta 5 upgrade validation. The affected Mac still needs local binary, code-signing, architecture, and termination-log inspection before assigning a root cause.
- The affected Mac's later crash report established launch-time code-signing enforcement: `EXC_CRASH`, `SIGKILL (Code Signature Invalid)`, `Taskgated Invalid Signature`, namespace `CODESIGNING`, exit 137. This confirms the symptom is not a managed application exception or memory/resource kill. The exact origin of the invalid taskgated state remains unproven, but the installer now avoids in-place Mach-O overwrite and re-seals the staged macOS binary before replacement.
- A local safe-read audit found `/Users/john/.DevCraft/devcraft` contained the 11-byte text fixture `new binary`. The installer regression test was changed to use real platform executables instead of text fixtures, and the installer now rejects non-platform executable payloads before replacement.
