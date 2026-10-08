# Feature Spec: Detect Current Folder Project State

## Feature Reference

- Feature ID: 0002
- Feature Name: Startup Soul And Folder Scan
- Type: Feature
- State: Implementation

Valid states: `Discovery`, `Clarification`, `Research`, `Planning`, `Analysis`, `Implementation`, `Complete`.
Code is only allowed while state is `Implementation`.

## Work Item

- Work Item Source: Operator conversation
- Work Item ID: N/A
- Work Item Title: Run startup soul setup and folder scan
- Work Item Summary: When the DevCraft CLI runs from a terminal, it should create `soul.md` if needed, inspect the current working folder, report whether the folder is ready for DevCraft or already has code, and for existing folders ask the default AI client to return JSON describing detected project names, project types, and whether an AI-driven SDLC is already in place.

## Overview

The DevCraft CLI will be run from the console inside a folder selected by the operator. Before creating files or scaffolding DevCraft artifacts, the CLI should understand whether the current folder is empty or already has user-visible contents. For this feature, "has code" means the folder contains any non-hidden file.

This feature also includes the startup environment check for DevCraft's installed profile folder and soul file. When DevCraft runs, it checks for the profile `.DevCraft` folder, then checks for `soul.md` inside that folder. If `soul.md` is missing, DevCraft asks the first-run soul questions and writes the answers to `soul.md`.

DevCraft should ask required startup questions through an agent-style terminal interface. The desired layout is similar to modern terminal AI tools: a persistent header at the top, a scrolling activity/transcript area in the middle, and a fixed input area at the bottom where the operator answers prompts.

This is the first step toward safer onboarding: DevCraft should not blindly create files without recognizing the current folder state.

## Goals

- Detect the current working folder when the CLI runs.
- Determine whether that folder contains any non-hidden files.
- Report the result clearly to the operator.
- Check whether the installed profile `.DevCraft` folder exists.
- Check whether `soul.md` exists inside the profile `.DevCraft` folder.
- Create `soul.md` from operator answers when it is missing.
- Detect installed terminal AI agents during soul setup.
- Store the operator's default terminal AI agent choice in `soul.md`.
- Present startup questions through a structured terminal interface.
- Keep the DevCraft identity visible while startup work and prompts are happening.
- Capture operator answers from a bottom input area.
- For folders with files, ask the default AI client to identify project types, project names, and existing AI-driven SDLC tooling.
- Display the detected project list in the console.

## Non-Goals

- Do not create DevCraft files in this feature.
- Create the profile `.DevCraft` folder only as part of first-run setup when it is missing.
- Do not modify existing project files in this feature.
- Do not initialize Git in this feature.
- Do not perform deep source-code analysis in this feature.
- Do not generate initial DevCraft project files in the scanned folder in this feature.
- Do not add microphone or voice input in this feature.

## Current State

The CLI currently displays the DevCraft logo and credit lines. It does not inspect the current directory, check the profile `.DevCraft` folder, create `soul.md`, call an AI client for folder scanning, or report detected project types.

## Target State

When the CLI runs, it:

1. checks whether the profile `.DevCraft` folder exists and creates it when missing during first-run setup
2. checks whether `soul.md` exists inside the profile `.DevCraft` folder
3. creates `soul.md` from first-run questions if it is missing
4. inspects the current working folder
5. if no non-hidden files exist, reports `Folder ready for DevCraft`
6. if non-hidden files exist, asks the default AI client to inspect the folder
7. receives JSON containing detected project names, project types, and AI-driven SDLC status
8. reports `Folder has code` and displays the detected project list
9. uses an agent-style terminal layout for questions, activity, and results

## Requirements

- Requirement 1: The CLI must read the current working folder from the running process context.
- Requirement 2: The CLI must determine whether the folder contains any non-hidden files.
- Requirement 3: The CLI must show a clear message stating whether the folder is empty or already contains files.
- Requirement 4: The CLI must not create, edit, move, or delete files as part of this detection feature.
- Requirement 5: Hidden files and hidden folders do not make an otherwise empty folder count as containing files.
- Requirement 5a: Any non-hidden folder entry makes the current folder count as having code, even if that folder has no files.
- Requirement 6: The CLI must check whether the profile `.DevCraft` folder exists.
- Requirement 7: The CLI must check whether `soul.md` exists inside the profile `.DevCraft` folder.
- Requirement 8: If the profile `.DevCraft` folder is missing during startup, the CLI must create it before soul setup.
- Requirement 9: If `soul.md` is missing, the CLI must ask first-run soul questions and write the answers to `soul.md`.
- Requirement 10: The CLI must support a terminal UI with a persistent top identity/header area, scrolling activity area, and bottom input area.
- Requirement 11: The CLI must ask startup questions in the activity area and receive answers through the bottom input area.
- Requirement 12: The CLI must operate based on operator answers.
- Requirement 13: If the current folder has no non-hidden files, the CLI must report `Folder ready for DevCraft`.
- Requirement 14: If the current folder has non-hidden files, the CLI must report `Folder has code`.
- Requirement 15: When the current folder has non-hidden files, DevCraft must ask the default AI client to inspect the folder.
- Requirement 16: Existing-folder inspection must ask the AI client to identify project names and project types.
- Requirement 17: Existing-folder inspection must ask whether an AI-driven SDLC platform is already in place, such as Superpowers or SpecKit.
- Requirement 18: The AI client must return JSON for existing-folder inspection.
- Requirement 19: The returned JSON must include a list of detected projects with project names and project types.
- Requirement 20: The CLI must display the detected project list in the console after inspection.
- Requirement 21: Soul setup must detect installed terminal AI agents before asking for a default agent.
- Requirement 22: Initial supported terminal AI agent detection must include Codex, Claude AI, and GitHub Copilot.
- Requirement 23: Soul setup must ask which detected terminal AI agent should be the default for console-launched AI work.
- Requirement 24: The selected default terminal AI agent must be recorded in `soul.md`.
- Requirement 25: The profile `.DevCraft` folder must contain support folders named `skills`, `standards`, and `architectures`.
- Requirement 26: The profile `.DevCraft` folder must contain `configure.json` with skills, standards, and architecture document lists and descriptions.
- Requirement 27: Each `configure.json` skill, standard, and architecture entry must include a unique lowercase kebab-case slug.
- Requirement 28: The CLI must support a `list` command that displays all configured skills, standards, and architectures when no category is provided.
- Requirement 29: The CLI must support `list skills`, `list standards`, and `list architectures` filters.
- Requirement 30: Support Markdown files such as `README.md` and underscore-prefixed templates must not be added to `configure.json` as usable skills, standards, or architectures.
- Requirement 31: The profile `.DevCraft` folder must contain a `templates` folder with `skill-template.md` copied from the shared skill template.
- Requirement 32: The profile `.DevCraft` folder must contain a `project-types` folder for reusable project type definitions.
- Requirement 33: `configure.json` must catalog templates and project types in addition to skills, standards, and architectures.
- Requirement 34: `configure.json` must include supported terminal clients with slug, name, description, binary path, and arguments.
- Requirement 35: The profile `.DevCraft` folder must contain `initialized.md` explaining how AI clients should use `soul.md`, `configure.json`, and DevCraft mode.
- Requirement 36: The profile `.DevCraft` folder root must contain `DevCraft.md` copied from the shared DevCraft mode rules.
- Requirement 37: The profile `.DevCraft` folder must contain a `feature-storage` folder for storage-mode instructions.
- Requirement 38: `configure.json` must include available feature storage types and the currently selected feature storage slug.
- Requirement 39: Each supported terminal client in `configure.json` must declare a scan operation object and a session operation object, each with binary path and arguments.
- Requirement 40: Codex scan operations must not force read-only sandbox mode, so future DevCraft scan operations can create or edit files when explicitly requested.

## Open Questions

- Should the CLI report only a yes/no result, or should it list what it found?
- Which terminal UI technology should DevCraft use for the persistent header, scrolling transcript, and bottom input area?
- Should the first implementation use a true full-screen terminal UI, or a simpler prompt-based approximation that can evolve later?
- What exact JSON schema should the AI client return?
- What is the exact definition of DevCraft's "default AI client" for automated project-learning prompts?
- Which existing AI-driven SDLC platforms should be detected first besides Superpowers and SpecKit?
- What executable names should be checked for Codex, Claude AI, and GitHub Copilot on each operating system?
- Should the default agent question show only installed agents, or also supported-but-not-installed agents?

## Clarification Log

### CL-001

- Question: What should the CLI detect when it runs from the current folder?
- Answer: It should tell the operator whether the folder already has code in it.
- Recommendation: N/A
- Decision: Start discovery for current-folder project-state detection.
- Spec Updates: Created this feature spec.

### CL-002

- Question: What should count as "the current folder already has code in it"?
- Answer: Any non-hidden file.
- Recommendation: Recognized project files plus source files was recommended, but the operator chose a broader empty-versus-existing-folder rule.
- Decision: Treat the folder as existing/non-empty when it contains any non-hidden file. Hidden files and hidden folders do not count.
- Spec Updates: Updated Overview, Goals, Target State, Requirements, Open Questions, user stories, and acceptance criteria.

### CL-003

- Question: Should the profile `.DevCraft` folder check and soul file check be included in this feature?
- Answer: Yes. That is what the operator wants in this feature.
- Recommendation: N/A
- Decision: Expand this feature to include startup checks for the profile `.DevCraft` folder and soul file.
- Spec Updates: Updated Work Item, Overview, Goals, Non-Goals, Current State, Target State, Requirements, Open Questions, user stories, and acceptance criteria.

### CL-004

- Question: How should DevCraft ask startup questions in the console?
- Answer: Use an agent-style console UI similar to Copilot or Codex: a top area that identifies DevCraft, a scrolling middle area showing what the AI agent is doing, and a bottom input area where the operator answers prompts.
- Recommendation: N/A
- Decision: Add the agent-style terminal UI requirement to this feature.
- Spec Updates: Updated Overview, Goals, Target State, Requirements, Open Questions, user stories, and acceptance criteria.

### CL-005

- Question: Should DevCraft ask which external AI client to use?
- Answer: For direct client launch interactions, yes, every time DevCraft is going to launch an external client for the rest of the interaction.
- Recommendation: N/A
- Decision: Do not treat AI client choice as a permanent soul-file default. Ask which client to use each time DevCraft needs to launch an external AI client.
- Spec Updates: Updated Requirements, Open Questions, acceptance criteria, and project discovery notes.

### CL-006

- Question: What should happen when DevCraft starts in a folder that already has files?
- Answer: It should check whether the folder has files. If files exist, DevCraft should ask the default AI to inspect the folder, determine whether it already uses another SDLC platform such as Superpowers or SpecKit, and if it does not, help build the initial DevCraft files to initialize DevCraft in that folder.
- Recommendation: N/A
- Decision: Add existing-folder AI inspection and DevCraft initialization planning to this feature.
- Spec Updates: Updated Requirements, Open Questions, acceptance criteria, and project discovery notes.

### CL-007

- Question: What is the exact startup flow for this feature?
- Answer: Run DevCraft from a repository folder. DevCraft checks for `soul.md`; if missing, it asks the soul questions and writes `soul.md`. Then it checks whether the current folder has non-hidden files. If not, it says `Folder ready for DevCraft`. If files exist, it asks the default AI client to inspect the folder and return JSON containing project types, project names, and whether an AI-driven SDLC is already in place. DevCraft then displays `Folder has code` and the detected project list in the console.
- Recommendation: N/A
- Decision: Feature 0002 includes soul creation, folder readiness detection, AI-backed existing-folder scan, JSON response handling, and console display of detected projects.
- Spec Updates: Updated Work Item, Overview, Goals, Non-Goals, Current State, Target State, Requirements, Open Questions, acceptance criteria, and project discovery notes.

### CL-008

- Question: During soul setup, should DevCraft detect installed terminal AI agents and collect the default agent?
- Answer: Yes. Soul setup should collect the default terminal AI agent, and DevCraft should detect installed agents, starting with Codex, Claude AI, and GitHub Copilot.
- Recommendation: N/A
- Decision: Include installed terminal AI agent detection and default agent selection in soul setup.
- Spec Updates: Updated Goals, Requirements, Open Questions, acceptance criteria, and project discovery notes.

### CL-009

- Question: Should this feature move from Discovery to Planning?
- Answer: Yes. Plan it out.
- Recommendation: N/A
- Decision: Move the feature to Planning and create `tasks.md`.
- Spec Updates: Updated feature state.

### CL-010

- Question: Should the approved task list move to Implementation?
- Answer: Yes. Implement it.
- Recommendation: N/A
- Decision: Move the feature to Implementation and execute the approved task list.
- Spec Updates: Updated feature state.

### CL-011

- Question: What should happen when the profile `.DevCraft` folder is missing?
- Answer: The first-run flow should create the profile folder and ask the soul setup questions.
- Recommendation: N/A
- Decision: Create missing profile `.DevCraft` during startup before checking or creating `soul.md`.
- Spec Updates: Updated Non-Goals, Target State, Requirements, and acceptance criteria.

### CL-012

- Question: Should code inside visible subfolders make the folder count as having code?
- Answer: Yes. A repository can have code in subfolders, and DevCraft should not report it as ready just because the top-level folder has no files.
- Recommendation: N/A
- Decision: Scan visible descendants recursively while ignoring hidden folders and generated output folders.
- Spec Updates: Updated requirements and tests.

### CL-013

- Question: Should a visible folder with no files count as code/existing content?
- Answer: Yes. If the current folder contains any non-hidden folder, it counts as existing content.
- Recommendation: N/A
- Decision: Treat any non-hidden folder entry as `Folder has code`, even when that folder is empty.
- Spec Updates: Updated requirements and tests.

## Research Readiness

- Internal code areas to inspect: `src/DevCraft.Cli`
- External APIs or dependencies to research: .NET file-system APIs for current directory and directory enumeration
- Known constraints: No implementation until this feature reaches `Implementation`.

## Standards Application

- Relevant shared standards from `codex-setup`: `/Users/john/codex-setup/standards/CSharp.md`
- How those standards apply to this feature: Future implementation must keep one hand-authored C# type per file, use clear names, and isolate file-system detection logic behind testable classes.
- Any project-specific standards extensions: DevCraft state gates remain binding.
- Any conflicts or special handling to account for: Detection should be read-only.

## New And Modified Views / Pages

- `CLI Project State Output` - `Modified`

## Module User Stories

### CLI Project Detection

#### Story 1: Report Current Folder Code State

- As an operator
- I want DevCraft to tell me whether the current folder contains any non-hidden files
- So that I know whether I am onboarding an existing project or starting from an empty folder

##### Happy Path Tests

- Test 1: A folder with at least one non-hidden file is reported as containing files.
- Test 2: An empty folder is reported as empty.

##### Edge Case Tests

- Test 1: A folder with only hidden files or hidden folders is reported as empty.

##### Negative Tests

- Test 1: Detection does not create, edit, move, or delete files.

### CLI Startup Environment

#### Story 1: Report Profile Folder And Soul File State

- As an operator
- I want DevCraft to check whether the profile `.DevCraft` folder and `soul.md` exist
- So that DevCraft can identify whether the installed runtime and first-run AI identity setup are ready

##### Happy Path Tests

- Test 1: A present profile `.DevCraft` folder is reported as present.
- Test 2: A present `soul.md` file inside the profile `.DevCraft` folder is reported as present.

##### Edge Case Tests

- Test 1: A missing profile `.DevCraft` folder is created before soul setup.
- Test 2: A present profile `.DevCraft` folder with no `soul.md` reports the folder as present and triggers soul setup.

##### Negative Tests

- Test 1: Startup environment detection does not create project files in the current folder.

#### Story 2: Create Missing Soul File

- As an operator
- I want DevCraft to ask first-run identity questions when `soul.md` is missing
- So that DevCraft can write local AI identity and operator-context guidance

##### Happy Path Tests

- Test 1: Missing `soul.md` triggers the first-run soul questions.
- Test 2: Operator answers are written to `soul.md`.
- Test 3: Installed terminal AI agents are detected during soul setup.
- Test 4: The selected default terminal AI agent is written to `soul.md`.

##### Edge Case Tests

- Test 1: Empty answers are handled according to the validation rules chosen during planning.

##### Negative Tests

- Test 1: DevCraft does not overwrite an existing `soul.md` during normal startup.

### CLI Agent Terminal UI

#### Story 1: Answer Startup Questions In A Structured Console

- As an operator
- I want DevCraft to show a persistent identity/header, scrolling activity, and bottom input area
- So that startup questions feel like an AI agent conversation rather than loose one-off prompts

##### Happy Path Tests

- Test 1: The DevCraft identity remains visible while startup questions are displayed.
- Test 2: Startup activity messages are shown in a transcript/activity area.
- Test 3: Operator input is collected from a bottom input area.

##### Edge Case Tests

- Test 1: The UI remains usable in a standard terminal window size selected during planning.

##### Negative Tests

- Test 1: The startup UI does not require microphone input.

### Existing Folder AI Inspection

#### Story 1: Scan Existing Folder With Default AI Client

- As an operator
- I want DevCraft to ask the default AI client to inspect an existing folder
- So that DevCraft can report what projects are present and whether another AI-driven SDLC is already in use

##### Happy Path Tests

- Test 1: A folder with non-hidden files triggers AI inspection.
- Test 2: AI inspection response JSON is parsed successfully.
- Test 3: Detected project names and project types are displayed in the console.

##### Edge Case Tests

- Test 1: AI inspection reports no detected projects.
- Test 2: AI inspection reports an existing AI-driven SDLC such as SpecKit or Superpowers.

##### Negative Tests

- Test 1: Invalid AI JSON is handled with a clear error.
- Test 2: AI inspection does not modify the scanned folder.

## Acceptance Criteria

- [x] The detection rule is clarified and recorded.
- [ ] The CLI reports `Folder ready for DevCraft` when the current folder has no non-hidden files.
- [ ] The CLI reports `Folder has code` when the current folder has non-hidden files.
- [ ] The CLI reports whether the profile `.DevCraft` folder exists.
- [ ] Missing profile `.DevCraft` is created before soul setup.
- [ ] The CLI checks whether `soul.md` exists inside the profile `.DevCraft` folder.
- [ ] Missing `soul.md` triggers first-run soul questions.
- [ ] First-run soul answers are written to `soul.md`.
- [ ] Soul setup detects installed terminal AI agents.
- [ ] Initial terminal AI agent detection includes Codex, Claude AI, and GitHub Copilot.
- [ ] Soul setup asks which detected terminal AI agent should be the default.
- [ ] The selected default terminal AI agent is written to `soul.md`.
- [ ] The CLI uses a terminal UI with persistent identity/header, scrolling activity, and bottom input area.
- [ ] Startup questions are shown in the activity area and answered through the bottom input area.
- [ ] Existing-folder startup uses an AI client to inspect for another SDLC platform.
- [ ] Existing-folder startup checks for Superpowers and SpecKit.
- [ ] Existing-folder AI inspection returns JSON containing detected project names and project types.
- [ ] Detected project names and project types are displayed in the console.
- [ ] Detection is read-only.
- [ ] Tests cover a folder with a non-hidden file, an empty folder, and a folder with only hidden files.
- [ ] Tests cover present/missing profile `.DevCraft` folder and present/missing soul file cases.

## Notes

The operator also mentioned that future work will add files the CLI can reference when creating DevCraft artifacts, and that existing folders will eventually trigger AI-assisted project learning. That creation/template and AI project-learning work is outside this feature unless the operator expands the scope.
