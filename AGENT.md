# AGENT.md

## Project Guidance

This repository uses DevCraft mode.

Shared cross-project guidance lives in `/Users/john/codex-setup` and remains the source of truth for reusable rules until this repository intentionally replaces that role.

Before meaningful project work:

1. Read `/Users/john/codex-setup/AGENT.md`.
2. Read `/Users/john/codex-setup/MODES.md`.
3. Read `/Users/john/codex-setup/modes/DevCraft.md`.
4. Read the relevant standards, architecture, tool, and design files from `/Users/john/codex-setup`.
5. Read this repository's DevCraft control files under `.devcraft/`.

## Active Mode

- Mode: `DevCraft`
- Project Status: `Discovery`
- Discovery Stage: `Brainstorming`
- Control Folder: `.devcraft/`

## Hard Gates

- Do not write application code unless an active DevCraft feature is in `Implementation`.
- Do not move project discovery stages, feature states, or implementation phases unless the operator explicitly asks.
- New material work starts with DevCraft discovery or a feature spec before implementation.
- Every hand-authored C# type must live in its own file once implementation begins.
- Automated tests must stay focused on one use case or business outcome per test.

## Current Project Direction

DevCraft is being developed as a full replacement for SpecKit and as a standalone repository that can manage documentation-first software delivery.

Initial product direction includes:

- refining the DevCraft rules currently living in `/Users/john/codex-setup`
- creating a reusable repository structure for DevCraft itself
- building .NET console applications to manage and run DevCraft workflows
- using Spectre.Console for polished console interfaces
- adding an ASCII-art capability for console banners or generated text

