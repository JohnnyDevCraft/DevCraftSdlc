# Architecture

## Purpose

Track the chosen architecture and important design decisions for DevCraft.

## Selected Architecture

- Primary Architecture: Undecided during project discovery
- Supporting Architecture: Planned .NET console tooling

## Modules or Layers

- Module 1: Workflow rules and templates
- Module 2: DevCraft project and feature artifact management
- Module 3: Console presentation and commands
- Module 4: Installation scripts and profile-folder runtime layout
- Module 5: Plugin and extension loading from the installed profile folder
- Module 6: First-run soul file setup and identity context
- Module 7: Agent-style terminal UI shell
- Module 8: External AI client adapters

## Integration Notes

- External systems: User profile filesystem
- Async processing: Not expected for the initial console MVP
- Data boundaries: Initial direction favors file-backed `.devcraft/` artifacts inside projects and a profile-level `.DevCraft` installation folder that contains runtime files, Markdown structure files, a soul file, and future plugin/extension content

## Notes

Architecture is still in the Brainstorming discovery stage. The initial implementation direction is a .NET console application using Spectre.Console, with an ASCII-art capability for terminal branding or text rendering.

Terminal UI direction: DevCraft should move toward an agent-style console interface with a persistent top identity/header area, a scrolling middle activity/transcript area, and a bottom input area for operator messages and answers. The operator specifically wants a Copilot/Codex-like terminal experience where DevCraft prompts with a question, the operator answers below, and DevCraft operates based on those answers.

External AI client direction: DevCraft may integrate with installed AI clients through adapter-style wrappers. Candidate clients are Codex CLI, Claude Code, and GitHub Copilot CLI. Initial integration should prefer scriptable/non-interactive modes when available, keep permissions explicit, and surface command activity in the DevCraft terminal transcript.

External client launch direction: DevCraft should support direct handoff by launching the selected AI CLI client in the current terminal window. In that mode, DevCraft prepares the required initial prompt or context, starts the external client process, and lets that client own the interactive terminal UI. DevCraft does not need to manage passthrough, follow-up messages, or the external client's conversation transcript beyond the initial prompt.

AI client selection direction: The external AI client is selected per interaction. When DevCraft is about to launch a client for the rest of an interaction, it asks the operator which available client to use, such as Codex, GitHub Copilot, or Claude AI. This is not stored as a permanent soul-file default.

Installation direction: DevCraft will live in this repository during development, then be installed through repo-local scripts. Linux and macOS use a shell script; Windows uses a PowerShell script. The install process copies required binary files plus Markdown structure files into a `.DevCraft` folder in the user's profile.

Extension direction: The installed profile `.DevCraft` folder will also be the load location for future DevCraft plugins and related add-ons. Exact plugin structure, discovery rules, trust model, and loading mechanism are not yet designed.

Soul direction: On launch, DevCraft first checks whether the installed profile `.DevCraft` folder exists, then checks for a soul file inside that folder. If the soul file does not exist, DevCraft prompts the operator to describe who they want their AI to be and who they are, so the AI can use that context when assisting. The first-run prompts are: `What should I call you?`, `Tell me what you do and how I'll be able to assist you.`, `What would you like me to be called?`, and `How would you like me to respond to you?` The soul file is an operator-authored overview of the AI identity and the person it works for, similar to the local Jarvis soul file used in this workspace. Exact file name, schema, and update workflow are not yet designed.

Default terminal AI agent direction: Soul setup also detects installed terminal AI agents and asks which one should be the default for console-launched AI work. Initial detection should cover Codex, Claude AI, and GitHub Copilot. The selected default is recorded in `soul.md` and used for automated project-learning prompts unless a later flow asks the operator to override it.
