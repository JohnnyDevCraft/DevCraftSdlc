# Feature Tasks: Startup Soul And Folder Scan

## Feature Reference

- Feature ID: 0002
- Feature Name: Startup Soul And Folder Scan
- Spec: [spec.md](./spec.md)
- Research: N/A for initial implementation; dependency and terminal-agent command checks are included in Phase 1.
- State: Implementation

## Planning Summary

- Planning goal: Extend the DevCraft CLI startup flow so it can create `soul.md`, detect installed terminal AI agents, select a default agent, inspect the current folder, and display AI-backed project detection results for folders with files.
- Recommended implementation sequence: Build testable services first, wire them into the CLI flow, then validate with folder fixtures and mocked AI JSON responses.
- Known dependencies: Existing DevCraft CLI project, Spectre.Console, filesystem APIs, process/command detection APIs, and a default terminal AI agent launcher.

## Phases

### Phase 1: Startup Context And Profile Paths

- Purpose: Establish the profile `.DevCraft` location and startup context model.
- Expected Outcome: The CLI can resolve the profile `.DevCraft` folder, current working folder, and `soul.md` path without mutating project folders.
- Entry Criteria: Operator approves this task list and moves the feature to `Implementation`.
- Exit Criteria: Startup context services are implemented and covered by focused tests.

#### Application Development

- [x] TASK-001 Create a startup context model for current folder, profile `.DevCraft` folder, and `soul.md` path.
- [x] TASK-002 Add a profile path resolver for the user's home/profile folder.
- [x] TASK-003 Add a read-only profile `.DevCraft` folder existence check.
- [x] TASK-004 Add a `soul.md` existence check inside the profile `.DevCraft` folder.
- [x] TASK-004A Ensure the profile `.DevCraft` folder contains `skills`, `standards`, and `architectures` support folders.
- [x] TASK-004B Copy base skills, standards, and architecture files into the profile `.DevCraft` support folders.
- [x] TASK-004C Write profile `configure.json` cataloging skills, standards, and architecture documents with descriptions.
- [x] TASK-004D Add lowercase kebab-case slugs to `configure.json` catalog entries.
- [x] TASK-004E Add `list` command support for all catalog entries or a specific category.
- [x] TASK-004F Exclude support Markdown files such as `README.md` and `_template.md` from `configure.json`.
- [x] TASK-004G Copy the shared skill template into `templates/skill-template.md`.
- [x] TASK-004H Create the profile `project-types` folder.
- [x] TASK-004I Add templates and project types to `configure.json`.
- [x] TASK-004J Add supported terminal clients to `configure.json`.
- [x] TASK-004K Create profile `initialized.md` with DevCraft AI client handoff guidance.
- [x] TASK-004L Copy shared DevCraft mode rules into profile root `DevCraft.md`.
- [x] TASK-004M Create the profile `feature-storage` folder.
- [x] TASK-004N Add feature storage types and selected feature storage to `configure.json`.
- [x] TASK-004O Split supported terminal client configuration into scan and session operation objects.
- [x] TASK-004P Remove forced read-only sandbox arguments from Codex scan operations.

#### Tests And Validation

- [x] TEST-001 Verify profile `.DevCraft` path resolution.
- [x] TEST-002 Verify present and missing profile `.DevCraft` folder detection.
- [x] TEST-003 Verify present and missing `soul.md` detection.
- [x] TEST-003A Verify profile support folders are created when missing.
- [x] TEST-003B Verify profile base files are copied and `configure.json` is written.
- [x] TEST-003C Verify generated slugs are lowercase kebab-case.
- [x] TEST-003D Verify list category parsing supports skills, standards, architectures, and all.
- [x] TEST-003E Verify `README.md` and underscore-prefixed template files are not cataloged.
- [x] TEST-003F Verify `templates/skill-template.md` is created.
- [x] TEST-003G Verify the profile `project-types` folder is created.
- [x] TEST-003H Verify `configure.json` includes templates, project types, and supported terminal clients.
- [x] TEST-003I Verify profile `initialized.md` is created.
- [x] TEST-003J Verify profile root `DevCraft.md` is created.
- [x] TEST-003K Verify the profile `feature-storage` folder is created.
- [x] TEST-003L Verify `configure.json` includes feature storage types and selected feature storage.
- [x] TEST-003M Verify supported clients include scan and session operation objects.
- [x] TEST-003N Verify Codex scan operation does not force read-only mode.

### Phase 2: Soul Setup And Default Agent Selection

- Purpose: Ask first-run soul questions and write `soul.md` when missing.
- Expected Outcome: Missing `soul.md` triggers the approved questions, detects installed terminal AI agents, records the selected default agent, and writes `soul.md`.
- Entry Criteria: Phase 1 is complete.
- Exit Criteria: Soul setup is testable without requiring real terminal input.

#### Application Development

- [x] TASK-005 Create a soul setup flow with the approved questions.
- [x] TASK-006 Add installed terminal AI agent detection for Codex, Claude AI, and GitHub Copilot.
- [x] TASK-007 Ask the operator which detected terminal AI agent should be the default.
- [x] TASK-008 Write `soul.md` with operator identity, AI identity/personality, assistance context, and selected default agent.
- [x] TASK-009 Prevent normal startup from overwriting an existing `soul.md`.

#### Tests And Validation

- [x] TEST-004 Verify missing `soul.md` triggers soul setup.
- [x] TEST-005 Verify soul answers are written to `soul.md`.
- [x] TEST-006 Verify installed-agent detection reports Codex, Claude AI, and GitHub Copilot availability.
- [x] TEST-007 Verify selected default terminal AI agent is written to `soul.md`.
- [x] TEST-008 Verify existing `soul.md` is not overwritten.

### Phase 3: Current Folder Readiness Detection

- Purpose: Determine whether the current working folder is empty or already has visible files.
- Expected Outcome: The CLI reports `Folder ready for DevCraft` for folders with no non-hidden files and `Folder has code` for folders with at least one non-hidden file.
- Entry Criteria: Phase 2 is complete.
- Exit Criteria: Folder detection works with empty, hidden-only, and non-hidden file fixtures.

#### Application Development

- [x] TASK-010 Create a read-only current-folder scanner.
- [x] TASK-011 Treat folders with no non-hidden files as ready for DevCraft.
- [x] TASK-012 Treat folders with at least one non-hidden file as folders with code.
- [x] TASK-013 Ignore hidden files and hidden folders for the empty-folder decision.
- [x] TASK-014 Display `Folder ready for DevCraft` or `Folder has code` in the console.

#### Tests And Validation

- [x] TEST-009 Verify an empty folder reports ready for DevCraft.
- [x] TEST-010 Verify a hidden-only folder reports ready for DevCraft.
- [x] TEST-011 Verify a folder with a non-hidden file reports folder has code.
- [x] TEST-012 Verify folder detection is read-only.

### Phase 4: Existing Folder AI Scan

- Purpose: Ask the default terminal AI agent to inspect folders that already contain files and return structured JSON.
- Expected Outcome: DevCraft can launch or invoke the configured default AI agent with an initial project-inspection prompt and parse the JSON result.
- Entry Criteria: Phase 3 is complete.
- Exit Criteria: AI scan prompt, JSON contract, parser, and failure handling are implemented with mocked test coverage.

#### Application Development

- [x] TASK-015 Define the existing-folder AI inspection prompt.
- [x] TASK-016 Define the JSON response contract for detected projects and AI-driven SDLC status.
- [x] TASK-017 Add a default terminal AI agent launcher abstraction.
- [x] TASK-018 Implement Codex launcher support first when Codex is installed.
- [x] TASK-019 Parse AI JSON into detected project records.
- [x] TASK-020 Handle invalid, missing, or incomplete AI JSON with a clear console error.

#### Tests And Validation

- [x] TEST-013 Verify the AI inspection prompt asks for project names, project types, and AI-driven SDLC status.
- [x] TEST-014 Verify valid AI JSON parses into detected project records.
- [x] TEST-015 Verify invalid AI JSON produces a clear failure result.
- [x] TEST-016 Verify existing-folder scan does not modify the scanned folder.

### Phase 5: Console Result Display

- Purpose: Display startup scan results in the DevCraft terminal UI.
- Expected Outcome: The operator sees the startup status, folder readiness/code status, detected project list, and AI-driven SDLC status.
- Entry Criteria: Phase 4 is complete.
- Exit Criteria: Console output is readable, scoped to the feature, and covered by tests where practical.

#### Application Development

- [x] TASK-021 Display profile `.DevCraft` and `soul.md` status in the activity transcript.
- [x] TASK-022 Display `Folder ready for DevCraft` for empty folders.
- [x] TASK-023 Display `Folder has code` for folders with files.
- [x] TASK-024 Display detected project names and project types from AI JSON.
- [x] TASK-025 Display whether an AI-driven SDLC platform was detected.

#### Tests And Validation

- [x] TEST-017 Verify empty-folder console output includes `Folder ready for DevCraft`.
- [x] TEST-018 Verify existing-folder console output includes `Folder has code`.
- [x] TEST-019 Verify detected project names and project types are included in output.
- [x] TEST-020 Verify AI-driven SDLC status is included in output.

### Final Phase: Validation And Completion

- Purpose: Prove the feature is complete and ready for handoff.

#### Application Validation

- [x] VAL-001 Run the relevant build/compile validation successfully.
- [x] VAL-002 Run all relevant automated tests successfully.
- [x] VAL-003 Run the CLI against an empty temporary folder and inspect the output.
- [x] VAL-004 Run the CLI against a temporary folder with a non-hidden file and mocked or controlled AI JSON response.
- [x] VAL-005 Record created and modified files in `results.md`.

#### UI Validation

- [x] VAL-006 Verify the terminal output remains readable in a standard terminal size.
- [x] VAL-007 Capture or preserve final CLI output evidence for review.
- [x] VAL-008 Confirm no browser or persistence validation is required for this console-only feature.
