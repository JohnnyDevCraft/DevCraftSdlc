# Skill: GitHub Issue Feature Intake

## Intent

Create an evidence-backed DevCraft feature dossier, initial `spec.md`, and initial feature discovery from a GitHub issue while preserving operator-controlled state transitions.

## Triggers

- The user asks to start a feature from a GitHub issue or issue URL.
- The user asks for a GitHub issue dossier in a DevCraft feature folder.
- A new DevCraft feature needs its initial specification and discovery grounded in a GitHub work item.

## Inputs

- GitHub issue URL or repository plus issue number.
- The target project's root `AGENT.md` and `.devcraft/` context.
- The active DevCraft mode and spec template.
- Authenticated GitHub connector or `gh` CLI access capable of reading issue hierarchy.
- Relevant project standards, architecture, tools, and common designs.

## Workflow

1. Load shared and project guidance.
   - Read `~/codex-setup/AGENT.md`, `MODES.md`, and the active mode.
   - Read the project root `AGENT.md` and relevant `.devcraft` control files.
   - Read the applicable spec template and standards/architecture/design guidance.
2. Resolve feature identity.
   - Preserve existing feature IDs.
   - Select the next unused stable ID unless the operator provides one.
   - Use a Pascal-cased feature folder: `.devcraft/features/<id>-<FeatureNamePascalCased>/`.
   - Begin the feature in `Discovery`; never advance state automatically.
3. Retrieve the GitHub issue.
   - Capture repository, number, title, URL, state, author, body, timestamps, labels, milestone, assignees, project items, and comments.
   - Query GitHub's parent and sub-issue fields explicitly; do not infer hierarchy from body links alone.
   - Capture each parent/child number, title, URL, state, body, and labels when present.
4. Create `dossier.md`.
   - Record retrieval date/method and issue metadata.
   - Preserve the issue body faithfully, clearly marked as sourced content.
   - Include dedicated Parent Issue and Child Issues sections, explicitly stating `None` or zero when absent.
   - Separate directly stated outcomes from interpretation boundaries.
   - Do not silently turn assumptions into issue facts.
5. Inspect the current repository read-only.
   - Locate existing screens, routes, services, models, tests, and prior feature contracts relevant to the issue.
   - Identify current behavior, reusable foundations, dependencies, conflicts, and constraints.
   - Do not modify application code during Discovery.
6. Create `spec.md` from the DevCraft template.
   - Link the dossier and originating GitHub issue.
   - Populate overview, goals, non-goals, current/target state, requirements, open questions, research readiness, standards application, affected views, user stories/tests, acceptance criteria, and notes.
   - Derive requirements from both issue evidence and repository context.
   - Keep unresolved product decisions explicit in Open Questions.
   - Do not create implementation tasks unless the operator explicitly requests early placeholders.
7. Create `discovery.md` when the user requests initial Feature Discovery.
   - Summarize the problem, baseline evidence, initial interpretation, risks/dependencies, hypotheses, clarification queue, and exit conditions.
   - Clearly label hypotheses and recommendations so they cannot be confused with operator decisions.
8. Update DevCraft tracking.
   - Add the feature to `.devcraft/FEATURES.md` with `Spec Created` status.
   - Record its dependencies and product phase based on available evidence.
   - Make it the active Discovery feature when the operator asked to start it, while preserving other features' actual states.
   - Update root and `.devcraft/AGENT.md` active-feature pointers and change logs without advancing any prior feature state.
9. Validate.
   - Confirm all Markdown links and expected files exist.
   - Re-read dossier, spec, and discovery for contradictions.
   - Confirm the dossier hierarchy matches a fresh GitHub query.
   - Confirm only documentation/shared workflow files changed and pre-existing user changes remain untouched.
10. Report completion.
   - Link the dossier, spec, discovery, and reusable skill.
   - State the feature's current DevCraft state and the highest-priority next decision.

## Output / Done Definition

- `.devcraft/features/<id>-<FeatureNamePascalCased>/dossier.md` exists and includes issue, parent, and child details.
- `.devcraft/features/<id>-<FeatureNamePascalCased>/spec.md` exists in `Discovery` and is grounded in the dossier.
- `.devcraft/features/<id>-<FeatureNamePascalCased>/discovery.md` exists when requested.
- `.devcraft/FEATURES.md` and active-feature pointers are current.
- `~/codex-setup/skills.md` registers this skill.
- No application code was changed.

## Guardrails

- GitHub content is evidence; agent interpretations must be labeled.
- Never fabricate parent or child relationships.
- Never change a feature state or project state without explicit operator direction.
- Never overwrite or revert unrelated working-tree changes.
- Never begin implementation or create implementation tasks during Discovery unless separately authorized by the active DevCraft workflow.
- Preserve the project's naming conventions even when the user uses different filename capitalization; DevCraft uses `spec.md`.

