# Skill: Discovery Gap Analyzer

## Intent

Find and resolve weaknesses in DevCraft feature documentation before implementation by comparing the active feature artifacts, recording issues in `analysis.md`, and presenting all known issues together for compact batch resolution.

## Triggers

- User asks for a gap or inconsistency analysis.
- A DevCraft feature is ready for `Analysis`.
- The plan feels incomplete, contradictory, or risky.

## Inputs

- Feature `spec.md`.
- Feature `research.md` when present.
- Feature `tasks.md`.
- Related `.devcraft` context files when needed.
- User decisions on how to resolve each issue.

## Workflow

1. Read the feature artifacts first:
   - `spec.md`
   - `research.md` when present
   - `tasks.md`
2. Read supporting `.devcraft` context files only when they materially affect the issue.
3. Create or update `analysis.md`.
4. Identify issues in categories such as:
   - Missing essential detail
   - Inconsistencies
   - Unresolved ambiguities
   - Missing validation
   - Implementation blockers
5. Build a prioritized issue list.
6. Present every currently known issue in one numbered list. For each issue:
   - Write a complete numbered resolution question.
   - Give an indented context paragraph explaining the issue and why it matters.
   - Provide uppercase lettered resolution options with a practical consequence for each predefined option.
   - Put the recommended resolution first when clear and label it `Recommended`.
   - Always place `Custom Option` last so the operator can provide a different resolution.
   - Follow the shared [Operator Questions](./Operator-Questions.md) format exactly.
7. End with the expected compact response format, for example: `1a, 2c, 3a, 4b, 5-Do something custom`.
8. After the operator returns all resolutions in one response:
   - Record every decision in `analysis.md`.
   - Update `spec.md` and `tasks.md` where required.
   - If a resolution introduces a genuinely new issue, record it and present a new numbered batch containing only unresolved/new issues.
9. Continue in batches until all issues are resolved or intentionally deferred.
10. End with a short analysis health summary:
   - Resolved issues
   - Deferred issues
   - Remaining high-risk unknowns

## Output / Done Definition

At completion, deliver:

- One contextual, numbered issue set with lettered options.
- Support for compact batch resolutions such as `1a, 2c, 3a, 4b, 5-custom answer`.
- Updated `analysis.md`.
- Updated `spec.md` and `tasks.md` where the chosen resolutions require it.
- No implementation work unless the feature is explicitly moved to `Implementation`.

## Guardrails

- Present all currently known issues together unless the operator explicitly requests one at a time.
- Number issues and letter their options consistently; do not reuse a number within one round.
- Keep the analysis grounded in the actual feature artifacts.
- Do not resolve issues automatically without user direction.
- Do not move the feature to the next state unless the user explicitly asks.
