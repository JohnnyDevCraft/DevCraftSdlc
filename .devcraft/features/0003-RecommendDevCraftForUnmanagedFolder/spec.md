# Feature Spec: Recommend DevCraft For Unmanaged Folder

## Feature Reference

- Feature ID: 0003
- Feature Name: Recommend DevCraft For Unmanaged Folder
- Type: Feature
- State: Implementation

Valid states: `Discovery`, `Clarification`, `Research`, `Planning`, `Analysis`, `Implementation`, `Complete`.
Code is only allowed while state is `Implementation`.

## Work Item

- Work Item Source: Operator conversation
- Work Item ID: N/A
- Work Item Title: Install DevCraft when no AI-driven SDLC is detected
- Work Item Summary: After the startup folder scan detects project contents, DevCraft should determine whether an AI-driven SDLC workflow is already present. If one is not present, DevCraft should install DevCraft as the workflow for the current folder.

## Overview

Feature 0002 scans a folder and reports detected project names, project types, and AI-driven SDLC status from the default terminal AI agent. This feature builds on that result by deciding what recommendation DevCraft should make next.

If the scan finds an existing AI-driven SDLC platform, such as SpecKit or Superpowers, DevCraft should report that existing workflow instead of trying to replace it silently. If no AI-driven SDLC is detected, DevCraft should initialize the folder with DevCraft.

## Goals

- Detect whether the scan result indicates an existing AI-driven SDLC workflow.
- Report existing SDLC workflow information clearly when present.
- Install DevCraft when no AI-driven SDLC workflow is detected.
- Avoid replacing or overwriting existing project files while installing DevCraft.

## Non-Goals

- Do not replace or migrate another SDLC workflow in this feature.
- Do not overwrite existing project files in this feature.
- Do not replace existing non-DevCraft SDLC markers.

## Current State

The CLI can scan an existing folder and display project names, project types, and whether an AI-driven SDLC was detected. It does not yet turn that SDLC detection into a next-step recommendation.

## Target State

After folder scanning:

- If an AI-driven SDLC is detected, DevCraft displays the detected workflow name.
- If no AI-driven SDLC is detected, DevCraft installs its local control structure.
- The installation result is displayed in the console.

## Requirements

- Requirement 1: The CLI must inspect the AI-driven SDLC detection result from the folder scan.
- Requirement 2: If an AI-driven SDLC is detected, the CLI must display the workflow name.
- Requirement 3: If no AI-driven SDLC is detected, the CLI must install DevCraft.
- Requirement 4: The CLI must not overwrite existing project files as part of this feature.
- Requirement 5: The recommendation output must be visible in the console after project scan results.
- Requirement 6: The CLI must use deterministic file/folder markers where possible before relying on AI interpretation.
- Requirement 7: The CLI must surface evidence for the detected or missing SDLC when deciding whether to install.
- Requirement 8: SDLC workflow detection must not treat agent clients or agent configuration files as SDLC workflows.
- Requirement 9: Initial deterministic SDLC marker checks must include DevCraft and Spec Kit.
- Requirement 10: Agent configuration markers such as Codex, GitHub Copilot, and Claude Code may be reported separately as agent configuration, not as SDLC presence.
- Requirement 11: If any SDLC marker is present, the CLI must clearly say whether the detected SDLC is DevCraft.
- Requirement 12: If an SDLC marker is present but it is not DevCraft, the CLI must clearly name the detected non-DevCraft workflow.
- Requirement 13: If no SDLC marker is present and the AI scan does not identify an AI-driven SDLC, the CLI must create the DevCraft control folder and seed files.
- Requirement 14: When the AI scan returns a project name and description, the CLI must ask the operator whether to use each AI suggestion or enter a custom value before generating DevCraft files.
- Requirement 15: Repository DevCraft installation must create `.devcraft/configure.json` with project-specific DevCraft configuration.

## Open Questions

- Which workflow names should DevCraft recognize as AI-driven SDLC platforms besides SpecKit and Superpowers?
- What marker files and folders identify Superpowers or other AI-driven SDLC tools that do not have a stable documented project layout?
- How should AI-provided SDLC evidence be represented in JSON?

## Clarification Log

### CL-001

- Question: What should DevCraft do after it detects whether there is a running SDLC?
- Answer: Start the next feature. If there is no running SDLC, DevCraft should suggest DevCraft.
- Recommendation: N/A
- Decision: Create a new feature for recommending DevCraft when no AI-driven SDLC is detected.
- Spec Updates: Created this feature spec.

### CL-002

- Question: How should DevCraft determine whether a folder is already using an AI-driven SDLC?
- Answer: Use deterministic file/folder markers where possible, and combine those with the AI scan result. Do not rely on a blind guess.
- Recommendation: N/A
- Decision: The detection strategy should inspect known workflow markers, ask AI for SDLC evidence in the scan, and display the evidence behind the recommendation.
- Spec Updates: Updated Requirements, Open Questions, tests, and acceptance criteria.

### CL-003

- Question: What initial deterministic markers should DevCraft use?
- Answer: Use documented or locally controlled markers first: DevCraft `.devcraft/`; Spec Kit `.specify/`, `.specify/memory/constitution.md`, `.specify/templates/`, and `specs/*/spec.md`, `plan.md`, `tasks.md`; Claude Code `CLAUDE.md`, `.claude/settings.json`, `.claude/commands/`, `.claude/agents/`, `.claude/skills/`; GitHub Copilot `.github/copilot-instructions.md` and `.github/instructions/*.instructions.md`; Codex-style `AGENTS.md`, `AGENTS.override.md`, and `.codex/` guidance.
- Recommendation: N/A
- Decision: Start with these marker families and keep Superpowers as AI-evidence-first until stable markers are confirmed.
- Spec Updates: Added marker table and updated requirements.

### CL-004

- Question: Are Codex, GitHub Copilot, and Claude Code SDLC workflows?
- Answer: No. They are agent clients or agent configuration systems, not SDLC workflows.
- Recommendation: N/A
- Decision: Split detection into SDLC workflow detection and agent configuration detection. Only SDLC workflows should block or change a DevCraft recommendation.
- Spec Updates: Updated requirements and marker tables.

### CL-005

- Question: Should feature 0003 implement marker scanning now?
- Answer: Yes. Scan for those markers as part of this feature.
- Recommendation: N/A
- Decision: Move feature 0003 into implementation for deterministic SDLC marker scanning and separate agent configuration marker reporting.
- Spec Updates: Updated feature state.

### CL-006

- Question: What should DevCraft report when an SDLC marker is already present?
- Answer: The next thing the operator needs to know is whether the detected marker is DevCraft.
- Recommendation: N/A
- Decision: When SDLC markers are present, report whether the workflow is DevCraft or a different detected SDLC.
- Spec Updates: Updated requirements, tasks, and acceptance criteria.

### CL-007

- Question: What should DevCraft do when no SDLC workflow is found?
- Answer: Automatically install DevCraft.
- Recommendation: N/A
- Decision: Replace recommendation-only behavior with automatic DevCraft initialization when no deterministic SDLC marker and no AI-reported SDLC is present.
- Spec Updates: Updated goals, non-goals, requirements, and acceptance criteria.

### CL-008

- Question: Should DevCraft automatically use the AI-provided project name and description when generating base files?
- Answer: No. DevCraft should show the AI-provided value as the first option and let the operator enter a custom value as the second option.
- Recommendation: N/A
- Decision: Before installing DevCraft for an existing-code folder, ask `What should we call this project?` and `Tell me what this project is about.` using Spectre selection prompts. Only use the AI value when the operator chooses it.
- Spec Updates: Updated requirements and acceptance criteria.

## Initial Marker Candidates

### SDLC Workflow Markers

| SDLC Workflow | Strong Markers | Supporting Markers | Confidence |
| --- | --- | --- | --- |
| DevCraft | `.devcraft/`, `.devcraft/AGENT.md`, `.devcraft/features/` | root `AGENT.md` naming DevCraft mode | High |
| Spec Kit | `.specify/`, `.specify/memory/constitution.md`, `.specify/templates/` | `specs/*/spec.md`, `specs/*/plan.md`, `specs/*/tasks.md`, `.github/prompts/` | High |
| Superpowers | TBD | Ask AI scan for evidence and candidate files until stable markers are confirmed | Low |

### Agent Configuration Markers

| Agent / Tool | Strong Markers | Supporting Markers | Notes |
| --- | --- | --- | --- |
| Claude Code | `CLAUDE.md`, `.claude/settings.json` | `.claude/commands/`, `.claude/agents/`, `.claude/skills/`, `.claude/hooks/` | Agent configuration, not SDLC |
| GitHub Copilot | `.github/copilot-instructions.md`, `.github/instructions/*.instructions.md` | `AGENTS.md` when used by Copilot-compatible agents | Agent configuration, not SDLC |
| Codex / AGENTS-style | `AGENTS.md`, `AGENTS.override.md`, `.codex/` | additional nested `AGENTS.md` files | Agent configuration, not SDLC |

## Research Readiness

- Internal code areas to inspect: `src/DevCraft.Cli/ConsoleProjectScanWriter.cs`, `src/DevCraft.Cli/ProjectScanResult.cs`, `src/DevCraft.Cli/AiSdlcDetection.cs`
- External APIs or dependencies to research: None expected
- Known constraints: No implementation until this feature reaches `Implementation`.

## Standards Application

- Relevant shared standards from `codex-setup`: `/Users/john/codex-setup/standards/CSharp.md`
- How those standards apply to this feature: Future implementation must keep one hand-authored C# type per file, maintain focused tests, and keep console output behavior testable.
- Any project-specific standards extensions: DevCraft state gates remain binding.
- Any conflicts or special handling to account for: This feature must remain read-only.

## New And Modified Views / Pages

- `CLI Project Scan Output` - `Modified`

## Module User Stories

### CLI Recommendation

#### Story 1: Install DevCraft For Unmanaged Folder

- As an operator
- I want DevCraft to install DevCraft when no AI-driven SDLC is already present
- So that a folder with no managed AI workflow is ready to use DevCraft immediately

##### Happy Path Tests

- Test 1: A scan result with no AI-driven SDLC installs DevCraft.
- Test 2: A scan result with an AI-driven SDLC displays the detected workflow name.
- Test 3: Known marker files or folders identify an existing SDLC workflow.

##### Edge Case Tests

- Test 1: A scan result with no projects and no AI-driven SDLC still installs DevCraft.

##### Negative Tests

- Test 1: Installation does not overwrite existing project files.
- Test 2: Installation is not performed without checking deterministic markers and AI scan evidence.

## Acceptance Criteria

- [x] Existing AI-driven SDLC names are displayed when detected.
- [x] Detected SDLC output clearly states whether DevCraft is present.
- [x] Detected non-DevCraft SDLC workflows are named.
- [x] Known SDLC workflow marker files and folders are checked.
- [x] Agent configuration markers are reported separately from SDLC workflow markers.
- [x] SDLC detection evidence is displayed or recorded with the install decision.
- [x] DevCraft is installed when no AI-driven SDLC is detected.
- [x] Installation output appears after scan results in the console.
- [x] Existing project files are not overwritten by installation.
- [x] AI-provided project name and description are operator-selectable suggestions, not automatically accepted.

## Notes

This feature now performs the initial DevCraft installation automatically when no SDLC workflow is found.
