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

## Notes

- Earlier `MongoDB.Driver 3.5.0` restore output reported vulnerable transitive `SharpCompress` and `Snappier` packages. Updating to `MongoDB.Driver 3.12.0` resolved those warnings in `dotnet list package --vulnerable --include-transitive`.
- Database migration code is implemented, but automated tests avoid requiring a live MongoDB instance. The configuration flow displays Docker setup instructions rather than starting Docker.
- PowerShell Core was not installed on the validation host, so `tests/InstallerReleaseSelection.Tests.ps1` was added but not executed locally.
