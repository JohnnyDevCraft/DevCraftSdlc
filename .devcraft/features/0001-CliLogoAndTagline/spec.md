# Feature Spec: CLI Logo And Tagline

## Feature Reference

- Feature ID: 0001
- Feature Name: CLI Logo And Tagline
- Type: Feature
- State: Implementation

Valid states: `Discovery`, `Clarification`, `Research`, `Planning`, `Analysis`, `Implementation`, `Complete`.
Code is only allowed while state is `Implementation`.

## Work Item

- Work Item Source: Operator conversation
- Work Item ID: N/A
- Work Item Title: DevCraft CLI ASCII-art logo and tagline
- Work Item Summary: Create the first DevCraft CLI branding feature: a polished ASCII-art `DevCraft` logo with `designed by Johnny DevCraft` beneath it.

## Overview

The DevCraft client CLI should have a strong branded first impression. The first feature is intentionally narrow: display a high-quality ASCII-art logo that says `DevCraft`, followed by the tagline `designed by Johnny DevCraft`.

This feature is being started early during project discovery as a bootstrap feature. Implementation remains blocked until the operator explicitly moves this feature to `Implementation`.

## Goals

- Define the branded CLI logo and tagline experience.
- Keep the first CLI feature small and clear.
- Establish the visual identity expectation for future console tooling.

## Non-Goals

- Do not implement the CLI project yet.
- Do not add a full welcome screen with status, version, commands, or next actions.
- Do not make branding themeable in this first feature.
- Do not choose the final ASCII-art generation dependency until research.

## Current State

DevCraft currently has project discovery artifacts and no implemented CLI application. The console tooling idea is captured in project discovery, but no feature implementation is authorized.

## Target State

When the DevCraft CLI is eventually implemented, its initial branded output includes:

- an ASCII-art logo spelling `DevCraft`
- `Dev` styled in green
- `Craft` styled in purple
- a copyright line underneath using the copyright symbol, 2026, and `Xelseor LLC`
- a creator credit line underneath saying `Designed and created by JohnnyDevCraft`
- presentation suitable for a polished command-line application

## Requirements

- Requirement 1: The CLI branding must include an ASCII-art `DevCraft` logo.
- Requirement 2: The logo must visually distinguish `Dev` in green and `Craft` in purple.
- Requirement 3: The CLI branding must show `© 2026 Xelseor LLC` below the logo.
- Requirement 4: The CLI branding must show `Designed and created by JohnnyDevCraft` below the copyright line.
- Requirement 3: The first feature scope is limited to logo and tagline presentation.
- Requirement 4: The feature must not add full dashboard, status, version, or action recommendation behavior.
- Requirement 5: The implementation plan must research Spectre.Console presentation and ASCII-art generation options before coding.

## Open Questions

- Should the ASCII-art logo be generated at runtime, stored as a static asset/string, or both?
- What exact ASCII-art style should the logo use?
- Should the colors be fixed terminal colors or later connected to DevCraft theming?

## Clarification Log

### CL-001

- Question: Should the CLI branding work start as DevCraft's first formal feature now, even though project discovery is still in Brainstorming?
- Answer: Start early feature discovery now.
- Recommendation: Start Early Feature Discovery.
- Decision: Create this first feature spec now while keeping code changes blocked until Implementation.
- Spec Updates: Added bootstrap context to Overview and Current State.

### CL-002

- Question: What should the first CLI branding feature include?
- Answer: Logo and tagline only.
- Recommendation: Logo And Tagline Only.
- Decision: Limit the first feature to a high-quality ASCII-art `DevCraft` logo and the tagline `designed by Johnny DevCraft`.
- Spec Updates: Added explicit Goals, Non-Goals, Requirements, and Acceptance Criteria for the narrow feature boundary.

### CL-003

- Question: What should the logo wordmark and credit styling be?
- Answer: The logo should say `DevCraft`, with `Dev` in green and `Craft` in purple. The credit line should use the copyright symbol, include 2026, and spell `JohnnyDevCraft` as one word.
- Recommendation: N/A
- Decision: Record the color split and one-word `JohnnyDevCraft` credit name as feature requirements.
- Spec Updates: Updated Target State, Requirements, Open Questions, user stories, and acceptance criteria.

### CL-004

- Question: What exact copyright and creator credit should appear below the logo?
- Answer: Copyright should read `© 2026 Xelseor LLC`, and the creator credit should say `Designed and created by JohnnyDevCraft`.
- Recommendation: N/A
- Decision: Use separate copyright and creator credit lines.
- Spec Updates: Updated Target State, Requirements, Open Questions, happy path tests, and acceptance criteria.

### CL-005

- Question: Should the feature move from Discovery to Planning?
- Answer: Yes. Make this the first feature, plan the tasks, show the task list, and wait for approval.
- Recommendation: N/A
- Decision: Move the feature to Planning and create `tasks.md`.
- Spec Updates: Updated feature state.

### CL-006

- Question: Should the approved task list move to Implementation?
- Answer: Yes. Build it.
- Recommendation: N/A
- Decision: Move the feature to Implementation and execute the approved tasks.
- Spec Updates: Updated feature state.

## Research Readiness

- Internal code areas to inspect: None yet; no CLI implementation exists.
- External APIs or dependencies to research: Spectre.Console, ASCII-art generation libraries or approaches for .NET.
- Known constraints: No application code may be created until this feature reaches `Implementation`.

## Standards Application

- Relevant shared standards from `codex-setup`: `/Users/john/codex-setup/standards/CSharp.md`
- How those standards apply to this feature: Future C# implementation must use one hand-authored type per file, standard .NET naming, nullable reference types, clear folder organization, and focused tests.
- Any project-specific standards extensions: DevCraft state gates remain binding.
- Any conflicts or special handling to account for: Bootstrap feature discovery is allowed early by operator decision, but implementation remains gated.

## New And Modified Views / Pages

- `CLI Logo Output` - `New`

## Module User Stories

### CLI Branding

#### Story 1: Display DevCraft Logo

- As an operator
- I want the DevCraft CLI to open with a polished ASCII-art `DevCraft` logo
- So that the tool has a clear and memorable identity

##### Happy Path Tests

- The CLI logo output includes ASCII art spelling `DevCraft`.
- `Dev` appears in green and `Craft` appears in purple.
- The copyright line `© 2026 Xelseor LLC` appears below the logo.
- The creator credit `Designed and created by JohnnyDevCraft` appears below the copyright line.

##### Edge Case Tests

- The logo remains readable in a standard terminal width selected during implementation planning.

##### Negative Tests

- The first feature does not display unrelated dashboard, repository status, or next-action content.

## Acceptance Criteria

- [ ] The feature spec defines an ASCII-art `DevCraft` logo requirement.
- [ ] The feature spec defines `Dev` as green and `Craft` as purple.
- [ ] The feature spec defines the copyright line as `© 2026 Xelseor LLC`.
- [ ] The feature spec defines the creator credit as `Designed and created by JohnnyDevCraft`.
- [ ] The feature scope excludes a full welcome screen, status dashboard, version display, and next-action recommendations.
- [ ] Research identifies how Spectre.Console and ASCII-art generation should be used.
- [ ] Implementation does not begin until the operator moves this feature to `Implementation`.

## Notes

This is the first DevCraft feature and is intentionally small. Future features can add a full welcome screen, themeable branding, repository status, workflow commands, and richer operator guidance.
