# Feature Results: Beta 11 Repository Upgrade Agent

## Feature Reference

- Feature ID: 0005
- Feature Name: Beta 11 Repository Upgrade Agent
- Spec: [spec.md](./spec.md)
- Tasks: [tasks.md](./tasks.md)
- Issues: [issues.md](./issues.md)
- State: Implementation

## Implementation Log

### Phase 1: Profile Default And Repository Override Feature Storage

- Started after the operator explicitly requested implementation.
- Added profile-level `DefaultFeatureStorage`, normalization, and initialization preservation.
- Added effective storage resolution so repository `SelectedFeatureStorage` acts as an optional override.
- Updated feature creation, listing, and launch context to use effective storage without prompting when the repository has no override.
- Renamed the Configure DevCraft menu action to `Change Repository-Level Feature Storage`.
- Added a `Use Profile Default` option that clears the repository override.

### Phase 2: Repository Version File

- Added current-version, repository-version, and prerelease comparison services.
- New installs now write `.devcraft/version` with `1.0.0-beta.11`.
- Missing or invalid repository versions are treated as legacy upgrade candidates.

### Phase 3: Force Upgrade Startup Flow

- Changed startup so `devcraft -force` can upgrade existing DevCraft folders before returning from the existing-install path.
- Current-version repositories report that no upgrade is required.
- Missing or older repository versions invoke the upgrade path.
- Failed upgrades leave `.devcraft/version` unchanged and stop normal launch.
- Successful upgrades write the current version and continue normal startup.

### Phase 4: Upgrade Agent Handoff

- Added a testable upgrade-agent launcher interface.
- Added a terminal upgrade launcher that selects a supported configured client and waits for completion.
- Added a structured upgrade prompt and result contract with success, summary, moved features, and skipped features.
- Kept the upgrade flow non-interactive after `devcraft -force` starts.

### Phase 5: Feature Catalog And GUID Migration Guidance

- Added prompt guidance for repository-central and system-central feature migrations.
- Documented GUID folder naming for both storage modes.
- Included instructions to update repository `.devcraft/configure.json` and profile `.DevCraft/features/projects.json`.
- Included instructions that migrated features must be browsable after upgrade and that the working directory remains the active codebase.

### Phase 6: Beta 11 Release Prep

- Bumped package metadata to `1.0.0-beta.11`.
- Updated README install examples and installer release-selection tests.

## Validation

- `dotnet test --filter "FullyQualifiedName~StartupFlowTests|FullyQualifiedName~DevCraftVersionComparerTests|FullyQualifiedName~DevCraftRepositoryUpgradePromptBuilderTests|FullyQualifiedName~ProfileConfigurationReaderTests|FullyQualifiedName~FeatureCommandTests|FullyQualifiedName~DevCraftMenuCommandTests|FullyQualifiedName~CliLogoRendererTests"`: passed, 63 tests.
- `dotnet test`: passed, 142 tests.
- `dotnet build -c Release`: passed.
- `tests/installer-release-selection.sh`: passed.
- `dotnet list package --vulnerable --include-transitive`: passed; no vulnerable packages reported.
- `git diff --check`: passed.

## Notes

- `dotnet test` and `dotnet build -c Release` currently report two existing xUnit analyzer warnings in `DevCraftMenuCommandTests.cs`.
