# Discovery

## Discovery Status

- Project Status: Discovery
- Discovery Stage: Brainstorming
- Valid Discovery Stages: Brainstorming | Architecture Planning | Model Design | Brand and Theming | Governance Design | Complete

## MVP Definition

- Core project purpose: Turn DevCraft into a standalone SpecKit replacement and reusable workflow system.
- Primary problem solved: DevCraft currently lives inside shared Codex setup, which makes it harder to version, package, evolve, and use as a dedicated project workflow tool.
- MVP outcome: A repository with DevCraft control files, refined rules, reusable templates, and an initial specified path for console tooling.

## Project Description

DevCraft is a documentation-first workflow for building software with AI assistance. It keeps project discovery, feature intent, research, planning, analysis, implementation history, and issue handling explicit and auditable.

This project will transform DevCraft from shared setup guidance into a standalone repository and tooling suite. The repository will eventually include console applications that help create, manage, validate, and run DevCraft workflows.

## Ubiquitous Language

- Term: Operator
- Meaning: The human decision-maker who controls DevCraft state transitions and implementation approval.

- Term: DevCraft Control Files
- Meaning: The `.devcraft/` files that act as the project source of truth.

- Term: Feature State
- Meaning: The explicit lifecycle state of a feature, such as Discovery, Planning, Analysis, or Implementation.

- Term: Implementation Phase
- Meaning: A task phase inside an approved feature implementation cycle.

- Term: SpecKit Replacement
- Meaning: A system intended to cover the workflow role currently associated with SpecKit while following DevCraft's own rules and preferences.

## Applications

- App 1: DevCraft console tooling for workflow management
- App 2: DevCraft documentation and template package
- App 3: DevCraft install scripts for Unix-like systems and Windows

## Modules

- Module 1: Project onboarding and control file management
- Module 2: Feature lifecycle management
- Module 3: Validation and audit reporting
- Module 4: Console presentation and operator interaction

## Tables

- Table 1: No database model selected during brainstorming
- Table 2: File-backed workflow artifacts are expected for the initial MVP

## Features

- Feature 1: DevCraft repository foundation
- Feature 2: CLI Logo And Tagline, started early as feature `0001-CliLogoAndTagline`
- Feature 3: Startup Soul And Folder Scan, started as feature `0002-DetectCurrentFolderProjectState`
- Feature 4: Recommend DevCraft For Unmanaged Folder, started as feature `0003-RecommendDevCraftForUnmanagedFolder`
- Feature 5: Console app foundation using .NET and Spectre.Console
- Feature 6: ASCII-art console branding or text rendering
- Feature 7: Profile-folder installation scripts
- Feature 8: Profile-folder plugin and extension loading
- Feature 9: First-run soul file setup
- Feature 10: Rule and template refinement from shared Codex setup

## Discovery Stage Notes

### Brainstorming

- Actors: Operator, AI agent, future project teams
- Requirements:
  - DevCraft must be usable as a standalone repository.
  - DevCraft must remain documentation-first.
  - DevCraft must preserve explicit operator-controlled state transitions.
  - Console tooling should improve usability without bypassing workflow gates.
  - Console UI should use Spectre.Console.
  - Console identity should support ASCII art.
- Use Cases:
  - Start a new DevCraft project.
  - Inspect project status and discovery stage.
  - Create and move feature artifacts through the lifecycle.
  - Validate that required artifacts exist before implementation.
  - Generate or display readable console workflow screens.
  - Install DevCraft into the user's profile folder.
  - Load DevCraft plugins and future extensions from the user's profile folder.
  - Prompt for first-run AI identity and operator context when no soul file exists.
  - Ask startup questions through an agent-style terminal UI with persistent header, scrolling activity, and bottom input.
  - Optionally delegate AI work to installed external AI clients such as Codex CLI, Claude Code, or GitHub Copilot CLI.
  - Pass through multi-turn conversations between the DevCraft terminal UI and the selected external AI client.
- MVP Features:
  - Repository control structure
  - Refined DevCraft operating rules
  - Console tooling spec
  - Implementation tasks for the first console app
  - Install scripts that place DevCraft runtime and Markdown structure files under the user's profile folder
  - Installed folder layout that can later host plugins and extensions
  - First-run check for a soul file in the installed profile `.DevCraft` folder
  - Agent-style terminal UI for first-run questions and operator responses
  - External AI client integration model
  - Conversation passthrough between DevCraft and selected AI clients

### Architecture Planning

- Languages: C# is planned for console tooling.
- Frameworks: .NET is planned for console applications.
- Tools: Spectre.Console is planned for terminal UI; ASCII-art library or approach is pending research; shell and PowerShell scripts are planned for installation.
- Architecture Decisions: Pending operator-controlled move to Architecture Planning.

### Model Design

- Core Entities: Pending
- Important Relationships: Pending
- Data Constraints: Pending

### Brand and Theming

- Theme Maker Project: Not started
- Exported Assets: None yet
- Theme Folder Status: Not started
- Follow-up Needed In `THEME.md`: Define console visual identity and ASCII-art usage.

### Governance Design

- Compliance Concerns: None identified yet
- Security Concerns: Avoid automation that mutates repositories, advances workflow state, or bypasses approval without explicit operator direction.
- Policy Constraints: AI must not implement code unless an active feature is in `Implementation`.
- Operational Constraints: DevCraft should remain auditable and source-controlled.

## Requirements

- Requirement 1: DevCraft must maintain explicit project status and discovery stage.
- Requirement 2: DevCraft must keep feature implementation gated by feature state.
- Requirement 3: DevCraft must support a future .NET console tooling experience.
- Requirement 4: Console tooling must use Spectre.Console unless discovery changes that decision.
- Requirement 5: Console tooling must include an ASCII-art capability unless discovery changes that decision.
- Requirement 6: DevCraft installation must be handled by repo-local install scripts.
- Requirement 7: Linux and macOS installation must use a shell script.
- Requirement 8: Windows installation must use a PowerShell script.
- Requirement 9: Install scripts must copy the required binary files and Markdown structure files into a `.DevCraft` folder in the user's profile.
- Requirement 10: DevCraft must be able to load future plugins and related extensions from the profile `.DevCraft` folder.
- Requirement 11: When DevCraft runs, it must check whether the profile `.DevCraft` folder exists.
- Requirement 12: After locating the profile `.DevCraft` folder, DevCraft must check for the soul file inside it.
- Requirement 13: If the soul file does not exist, DevCraft must prompt the operator to describe who they want their AI to be and who they are.
- Requirement 14: The soul file must give the AI enough identity and operator context to know who it is and who it is working for.
- Requirement 15: First-run soul setup must ask the operator `What should I call you?`
- Requirement 16: First-run soul setup must ask the operator `Tell me what you do and how I'll be able to assist you.`
- Requirement 17: First-run soul setup must ask the operator `What would you like me to be called?`
- Requirement 18: First-run soul setup must ask the operator `How would you like me to respond to you?`
- Requirement 19: DevCraft startup questions must be presented through a terminal UI with a persistent top identity/header area, scrolling activity area, and bottom input area.
- Requirement 20: When DevCraft needs to launch an external AI client for an interaction, it must ask which client to use.
- Requirement 21: External AI client choices should include Codex, GitHub Copilot, and Claude AI when available.
- Requirement 22: DevCraft should be able to integrate with installed external AI clients where they expose scriptable CLI interfaces.
- Requirement 23: Candidate external AI clients include Codex CLI, Claude Code, and GitHub Copilot CLI.
- Requirement 24: DevCraft must be able to prepare an initial prompt for a selected external AI client.
- Requirement 25: DevCraft must launch the selected external AI CLI client directly in the current terminal window with the initial prompt when supported.
- Requirement 26: After launch, DevCraft does not need to manage passthrough, follow-up messages, or the external client's conversation transcript.
- Requirement 27: DevCraft must keep responsibility for preparing the DevCraft-specific initial context and prompt before handoff.
- Requirement 28: When a new DevCraft session starts in a folder with non-hidden files, DevCraft must ask an AI client to inspect the folder for existing SDLC platforms such as Superpowers or SpecKit.
- Requirement 29: If no existing SDLC platform is detected in an existing folder, DevCraft must ask the AI client to help build the initial DevCraft files needed to initialize that folder.
- Requirement 30: Feature 0002 must create `soul.md` from first-run questions if it does not exist.
- Requirement 31: Feature 0002 must ask the default AI client to return JSON containing detected project names, project types, and AI-driven SDLC status for folders with files.
- Requirement 32: Feature 0002 must display detected project names and project types in the console.
- Requirement 33: Feature 0002 must detect installed terminal AI agents during soul setup.
- Requirement 34: Initial terminal AI agent detection must include Codex, Claude AI, and GitHub Copilot.
- Requirement 35: Feature 0002 must ask which detected terminal AI agent should be the default for console-launched AI work.
- Requirement 36: Feature 0002 must record the selected default terminal AI agent in `soul.md`.

## Use Cases

- Use Case 1: The operator starts a new repository and initializes DevCraft control files.
- Use Case 2: The operator plans console tooling through DevCraft before implementation.
- Use Case 3: The operator uses DevCraft to manage DevCraft's own lifecycle.
- Use Case 4: The operator installs DevCraft from this repository into their profile using the OS-appropriate install script.
- Use Case 5: DevCraft loads installed plugins and extensions from the user's profile `.DevCraft` folder.
- Use Case 6: On first run, DevCraft asks the operator to define the AI identity and operator context because no soul file exists yet.
- Use Case 7: The operator answers the first-run soul questions, and DevCraft uses the answers to write a soul file for future AI guidance.
- Use Case 8: The operator interacts with DevCraft through an agent-style terminal UI where questions appear in the transcript and answers are typed into the bottom input area.
- Use Case 9: DevCraft delegates project-learning or generation work to a configured AI client such as Codex CLI, Claude Code, or GitHub Copilot CLI.
- Use Case 10: The operator selects an external AI client; DevCraft prepares the initial prompt, launches that client's own CLI UI in the current terminal window, and then the client owns the interactive conversation.
- Use Case 11: During an existing-folder project-learning flow, DevCraft asks the operator which AI client to launch for that interaction, such as Codex, GitHub Copilot, or Claude AI.
- Use Case 12: DevCraft starts in an existing folder, asks an AI client to inspect whether the folder already uses Superpowers, SpecKit, or another SDLC workflow, then prepares DevCraft initialization if no conflicting workflow exists.
- Use Case 13: DevCraft starts in an empty folder and reports `Folder ready for DevCraft`.
- Use Case 14: DevCraft starts in a folder with files, asks the default AI client for structured JSON project detection, and reports `Folder has code` with detected project names and types.
- Use Case 15: DevCraft runs soul setup, detects installed terminal AI agents, asks the operator which one should be the default, and stores that choice in `soul.md`.

## Actors

- Actor 1: Operator
- Actor 2: AI agent
- Actor 3: Future developer using DevCraft in another project

## URLs and Links

- Link 1: `/Users/john/codex-setup`
- Link 2: `/Users/john/codex-setup/modes/DevCraft.md`

## Attachments

- Attachment 1: None
- Attachment 2: None

## Change Log

- 2026-10-07: Started DevCraft project discovery and recorded the initial direction to build DevCraft using DevCraft.
- 2026-10-07: Started early feature discovery for `0001-CliLogoAndTagline` with scope limited to ASCII-art `DevCraft` logo plus `designed by Johnny DevCraft` tagline.
- 2026-10-07: Refined the first logo direction: `Dev` in green, `Craft` in purple, with a copyright/credit line using `JohnnyDevCraft` as one word.
- 2026-10-07: Replaced the first logo credit with `© 2026 Xelseor LLC` and `Designed and created by JohnnyDevCraft`.
- 2026-10-07: Started discovery for `0002-DetectCurrentFolderProjectState`, a read-only CLI check that reports whether the current folder contains any non-hidden files.
- 2026-10-07: Chose the current-folder detection rule: any non-hidden file means the folder is treated as existing/non-empty; an empty folder starts the new-project path; existing folders later lead to AI-assisted project learning.
- 2026-10-07: Captured the install direction: repo-local shell script for Linux/macOS, PowerShell script for Windows, copying required binaries and Markdown structure files into a profile folder named `.DevCraft`.
- 2026-10-07: Captured profile-folder extension direction: future plugins and related add-ons load from the user's installed `.DevCraft` folder.
- 2026-10-07: Captured first-run soul setup: DevCraft checks the profile `.DevCraft` folder for a soul file and prompts for AI identity plus operator context when it is missing.
- 2026-10-07: Clarified the soul file purpose: it is a simple operator-authored overview of who the AI is and who it works for, similar to the local Jarvis soul file.
- 2026-10-07: Captured the first-run soul prompt sequence: what to call the operator, what they do and how the AI can assist, what the AI should be called, and how the AI should respond.
- 2026-10-07: Clarified run startup order: check for the profile `.DevCraft` folder first, then check for the soul file inside that folder.
- 2026-10-07: Folded profile `.DevCraft` folder and soul file startup checks into feature `0002-DetectCurrentFolderProjectState` by operator direction.
- 2026-10-07: Captured the desired startup question UI: persistent top DevCraft identity, scrolling middle activity/transcript, and bottom input area similar to terminal AI tools.
- 2026-10-07: Captured external AI client direction: DevCraft may integrate with installed scriptable clients such as Codex CLI, Claude Code, and GitHub Copilot CLI.
- 2026-10-07: Simplified external AI integration direction: DevCraft prepares the initial prompt, launches the selected external AI CLI client in the current terminal window, and does not manage passthrough beyond the initial prompt.
- 2026-10-07: Replaced default external AI client selection with per-run selection: DevCraft asks which client to launch each time it needs to hand off an interaction.
- 2026-10-07: Added existing-folder startup direction: use an AI client to inspect folders with files for existing SDLC platforms such as Superpowers or SpecKit, then prepare initial DevCraft files if none are found.
- 2026-10-07: Refined feature `0002-DetectCurrentFolderProjectState` into an end-to-end startup flow: create `soul.md` if missing, report `Folder ready for DevCraft` for empty folders, and for folders with files call the default AI client for JSON project detection, then display detected project names and types.
- 2026-10-07: Added soul setup requirement to detect installed terminal AI agents, starting with Codex, Claude AI, and GitHub Copilot, then store the selected default agent in `soul.md`.
- 2026-10-07: Started discovery for `0003-RecommendDevCraftForUnmanagedFolder`, a read-only recommendation feature that suggests DevCraft when no AI-driven SDLC is detected.
