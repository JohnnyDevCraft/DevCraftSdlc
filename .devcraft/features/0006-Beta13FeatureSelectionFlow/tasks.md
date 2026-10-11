# Feature Tasks: Beta 13 Feature Selection Flow

## Feature Reference

- Feature ID: 0006
- Feature Name: Beta 13 Feature Selection Flow
- Spec: [spec.md](./spec.md)
- State: Implementation

## Planning Summary

- Goal: Let operators back out of repository feature lists and delay AI client selection until after a feature has been selected.
- Primary touchpoints: `DevCraftMenuCommand`, `FeatureCommand`, feature menu tests, and version/install metadata.
- TDD approach: Add focused tests for feature-list back behavior and AI-client prompt order, then implement the smallest command/menu change that passes them.

## Phase 1: Existing Feature Selection Flow

- Purpose: Change existing-feature selection so feature choice happens before AI client choice.
- Expected Outcome: Feature listing has `Back`, backing out launches nothing, and AI client selection occurs only after selecting a feature.
- Entry Criteria: Operator explicitly authorizes implementation for Beta 13.
- Exit Criteria: Focused menu/feature command tests pass.

### Application Development

- [x] TASK-001 Add a `Back` option to repository feature list choices.
- [x] TASK-002 Return without launching an agent when `Back` is selected from the feature list.
- [x] TASK-003 Move AI client selection for `List features` so it happens after the selected feature is known.
- [x] TASK-004 Preserve the selected AI client in the feature AI session launcher call.
- [x] TASK-005 Preserve existing no-feature behavior without prompting for an AI client.

### Tests And Validation

- [x] TEST-001 Verify repository feature list includes `Back`.
- [x] TEST-002 Verify selecting `Back` from the feature list does not launch a feature AI session.
- [x] TEST-003 Verify selecting `Back` from the feature list does not prompt for an AI client.
- [x] TEST-004 Verify selecting an existing feature prompts for an AI client after feature selection.
- [x] TEST-005 Verify the selected AI client is passed to the feature AI session launcher.
- [x] TEST-006 Verify an empty feature list reports no features and does not prompt for an AI client.

## Phase 2: Beta 13 Release Metadata

- Purpose: Prepare the package and installer-visible metadata for Beta 13.
- Expected Outcome: DevCraft identifies itself and installs as `1.0.0-beta.13`.
- Entry Criteria: Phase 1 behavior is implemented and tested.
- Exit Criteria: Release metadata validation passes.

### Application Development

- [x] TASK-006 Bump package version to `1.0.0-beta.13`.
- [x] TASK-007 Update README install examples to `1.0.0-beta.13`.
- [x] TASK-008 Update installer release-selection tests to include `v1.0.0-beta.13`.
- [x] TASK-009 Record implementation and validation in `results.md`.

### Tests And Validation

- [x] TEST-007 Run focused feature selection tests.
- [x] TEST-008 Run `dotnet test`.
- [x] TEST-009 Run `dotnet build -c Release`.
- [x] TEST-010 Run `tests/installer-release-selection.sh`.
- [x] TEST-011 Run `git diff --check`.
