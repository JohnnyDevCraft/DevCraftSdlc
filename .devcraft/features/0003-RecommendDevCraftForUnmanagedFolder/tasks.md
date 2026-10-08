# Feature Tasks: Recommend DevCraft For Unmanaged Folder

## Feature Reference

- Feature ID: 0003
- Feature Name: Recommend DevCraft For Unmanaged Folder
- Spec: [spec.md](./spec.md)
- Research: N/A for this implementation pass; initial marker candidates are captured in `spec.md`.
- State: Implementation

## Planning Summary

- Planning goal: Scan deterministic SDLC workflow markers and agent configuration markers, display the evidence, and install DevCraft only when no SDLC workflow is detected.
- Recommended implementation sequence: Add marker models and scanner, wire marker results into project scan output, add guarded installer, then validate with fixture directories.
- Known dependencies: Existing startup folder scan and console project scan output.

## Phases

### Phase 1: Marker Model And Scanner

- Purpose: Represent and detect SDLC and agent configuration markers.
- Expected Outcome: DevCraft can scan a folder for DevCraft, Spec Kit, Claude Code, GitHub Copilot, and Codex/AGENTS-style markers.
- Entry Criteria: Operator approved marker scanning for this feature.
- Exit Criteria: Marker scanner has focused unit tests.

#### Application Development

- [x] TASK-001 Add marker category and marker result models.
- [x] TASK-002 Add deterministic marker definitions for DevCraft and Spec Kit SDLC workflows.
- [x] TASK-003 Add deterministic marker definitions for Claude Code, GitHub Copilot, and Codex/AGENTS-style agent configuration.
- [x] TASK-004 Implement a read-only marker scanner.

#### Tests And Validation

- [x] TEST-001 Verify DevCraft markers are detected as SDLC workflow markers.
- [x] TEST-002 Verify Spec Kit markers are detected as SDLC workflow markers.
- [x] TEST-003 Verify Claude Code markers are detected as agent configuration markers.
- [x] TEST-004 Verify GitHub Copilot markers are detected as agent configuration markers.
- [x] TEST-005 Verify Codex/AGENTS markers are detected as agent configuration markers.

### Phase 2: Detection Output

- Purpose: Display marker evidence and prepare installation when no SDLC workflow is present.
- Expected Outcome: Console output clearly separates SDLC workflows from agent configuration and installs DevCraft only when appropriate.
- Entry Criteria: Phase 1 marker scanner works.
- Exit Criteria: Console output tests cover detected and unmanaged folders.

#### Application Development

- [x] TASK-005 Add marker scanning to the existing folder scan flow.
- [x] TASK-006 Display detected SDLC workflow markers.
- [x] TASK-007 Display detected agent configuration markers separately.
- [x] TASK-008 Clearly report when the detected SDLC workflow is DevCraft.
- [x] TASK-009 Clearly report when a non-DevCraft SDLC workflow is detected.
- [x] TASK-010 Install DevCraft when no SDLC workflow marker is detected.
- [x] TASK-011 Avoid installing DevCraft when an SDLC workflow marker is detected.
- [x] TASK-012 Avoid overwriting existing project files during installation.
- [x] TASK-013 Prompt the operator to accept or override the AI-provided project name and description before installation.
- [x] TASK-014 Create repository `.devcraft/configure.json` during DevCraft installation.

#### Tests And Validation

- [x] TEST-006 Verify unmanaged folder output installs DevCraft.
- [x] TEST-007 Verify folder with DevCraft marker does not install DevCraft.
- [x] TEST-008 Verify folder with Spec Kit marker does not install DevCraft.
- [x] TEST-009 Verify agent configuration markers do not suppress DevCraft installation.
- [x] TEST-010 Verify detected SDLC output states whether DevCraft is present.
- [x] TEST-011 Verify DevCraft installation preserves existing files.
- [x] TEST-012 Verify operator custom project name and description are used instead of AI suggestions when selected.
- [x] TEST-013 Verify repository `.devcraft/configure.json` is created.

### Final Phase: Validation And Completion

- Purpose: Prove the feature is complete and ready for handoff.

#### Application Validation

- [x] VAL-001 Run the relevant build/compile validation successfully.
- [x] VAL-002 Run all relevant automated tests successfully.
- [x] VAL-003 Run the CLI against a controlled folder with no SDLC marker.
- [x] VAL-004 Run the CLI against a controlled folder with a Spec Kit marker.
- [x] VAL-005 Record created and modified files in `results.md`.

#### UI Validation

- [x] VAL-006 Verify marker output is readable in the console.
- [x] VAL-007 Confirm no browser or persistence validation is required for this console-only feature.
