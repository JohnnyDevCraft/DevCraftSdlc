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

## TDD Evidence

### Force Install And Root Agent Preservation

- Red: `dotnet test --filter "FullyQualifiedName~DevCraftInstallerTests|FullyQualifiedName~StartupFlowTests"` failed to compile because `StartupFlow.Run` had no `forceInstall` parameter after adding the first force-install tests.
- Red: `dotnet test --filter "FullyQualifiedName~StartupFlowTests"` failed `RunInstallsDevCraftWhenForcedInCodeFolderWithoutScanning` because force did not install in a code folder without SDLC markers.
- Green: `dotnet test --filter "FullyQualifiedName~DevCraftInstallerTests|FullyQualifiedName~StartupFlowTests"` passed with 13 focused tests after wiring `devcraft -force`, removing root `AGENT.md` generation, and making force bypass scans.
- Refactor: Shared/profile/live rules and templates were updated after the focused green run to make TDD evidence and root-agent preservation part of the reusable DevCraft workflow.

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

## Notes

- Earlier `MongoDB.Driver 3.5.0` restore output reported vulnerable transitive `SharpCompress` and `Snappier` packages. Updating to `MongoDB.Driver 3.12.0` resolved those warnings in `dotnet list package --vulnerable --include-transitive`.
- Database migration code is implemented, but automated tests avoid requiring a live MongoDB instance. The configuration flow displays Docker setup instructions rather than starting Docker.
- PowerShell Core was not installed on the validation host, so `tests/InstallerReleaseSelection.Tests.ps1` was added but not executed locally.
- The Beta 4 instant-kill report was not reproduced locally during isolated Beta 4 to Beta 5 upgrade validation. The affected Mac still needs local binary, code-signing, architecture, and termination-log inspection before assigning a root cause.
