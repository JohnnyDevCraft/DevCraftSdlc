# Feature Spec: Beta 13 Feature Selection Flow

## Feature Reference

- Feature ID: 0006
- Feature Name: Beta 13 Feature Selection Flow
- Requested Version: 1.0.0-beta.13
- State: Implementation

## Operator Request

When DevCraft lists features for a repository, the list should provide a back option. DevCraft should not ask the operator to select an AI client until after the operator selects a feature. Selecting the AI agent should be the final step before DevCraft passes control to the agent.

## Current Behavior

- The Features menu offers `List features`, `Create new feature`, and `Back`.
- When the operator chooses `List features`, the menu selects an AI client before calling the feature list command.
- The feature list only contains feature choices. It does not include a `Back` option.
- If the operator intended only to inspect or back out of the feature list, DevCraft still asks for the AI client too early.

## Target Behavior

- Repository feature listing includes a `Back` option.
- Choosing `Back` from the repository feature list returns to the previous feature menu without launching an agent.
- DevCraft does not select or prompt for an AI client before showing the feature list.
- DevCraft prompts for the AI client only after a concrete feature has been selected.
- AI client selection remains the last interactive step before launching the feature AI session.

## Requirements

- Requirement 1: The repository feature list must include `Back`.
- Requirement 2: Selecting `Back` from the feature list must not launch a feature AI session.
- Requirement 3: Selecting `Back` from the feature list must not prompt for an AI client.
- Requirement 4: Listing features from the menu must show the feature list before any AI client prompt.
- Requirement 5: Selecting a feature must prompt for the AI client after the feature is selected.
- Requirement 6: The selected AI client must be passed to the feature AI session launcher.
- Requirement 7: Empty feature lists must keep the existing no-features behavior and must not prompt for an AI client.
- Requirement 8: Creating a new feature may continue to select the AI client after the feature name is entered and before agent launch.
- Requirement 9: The package version and install examples must be bumped to `1.0.0-beta.13`.

## Non-Goals

- Do not redesign the full Features menu.
- Do not change feature storage selection behavior beyond preserving the current effective storage rules.
- Do not change supported AI client configuration.
- Do not change how feature folders or catalogs are stored.

## Acceptance Criteria

- Feature list choices include `Back`.
- A test proves that choosing `Back` from the feature list does not show `Which AI client should I use?`.
- A test proves that selecting an existing feature shows the AI client prompt after `Select a feature`.
- A test proves that the selected client is still used for the feature handoff.
- Full automated tests pass.
