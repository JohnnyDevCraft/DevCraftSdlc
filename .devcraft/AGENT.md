# AGENT.md

## Project Context

- Active Mode: `DevCraft`
- DevCraft Control Folder: `.devcraft/`
- Project Name: DevCraft
- Primary Goal: Build DevCraft into a full SpecKit replacement and standalone repository for documentation-first software delivery.
- Project Status: Discovery
- Valid Project Statuses: Discovery | Active | Maintenance
- Branching Model: Undecided
- Master Branch: Undecided

## Languages and Frameworks

- Languages: C# planned for console tooling
- Frameworks: .NET planned for console applications

## Architecture

- Selected Architecture: Undecided during discovery
- Notes: Early direction favors a .NET console tooling suite with a documentation/workflow core. Architecture decisions remain in discovery.

## Tools

- Relevant Tools: Spectre.Console planned; ASCII-art generation library or approach pending research
- Notes: Console UI tooling is a first-class project need, but implementation is gated behind DevCraft feature approval.

## Work Management

- Work Item Store: DevCraft artifacts under `.devcraft/`
- Work Item Tooling: DevCraft itself

## Collaboration

- Number of Contributors: Initially one operator with AI assistance
- Shared Repository Purpose: Reusable DevCraft project rules, workflow templates, and console tools

## Project Setup Notes

- Who Will Use It: John and future projects that need a structured AI-assisted specification workflow
- Why It Is Needed: The existing DevCraft rules live inside shared Codex setup and need to become a capable standalone system.
- Ubiquitous Language Notes: DevCraft, operator, project discovery, feature state, implementation phase, control files
- Governing Rules: DevCraft mode controls project discovery, feature design, implementation gating, and results tracking.
- Security Concerns: Avoid automating destructive repository or workflow changes without explicit operator approval.

## Current Priorities

- Priority 1: Define DevCraft as a SpecKit replacement.
- Priority 2: Establish the repository control structure and discovery record.
- Priority 3: Specify console tooling before implementing it.

## Discovery Tracking

- Discovery Stage: Brainstorming
- Valid Discovery Stages: Brainstorming | Architecture Planning | Model Design | Brand and Theming | Governance Design | Complete
- MVP Definition Status: In progress

## Active Feature Tracking

- Active Feature ID: 0004
- Active Feature Name: Situational Awareness Logs
- Active Feature State: Implementation
- Current Implementation Phase: Final Phase: Validation And Release Prep

## Working Agreements

- DevCraft control files live under `.devcraft/`.
- New projects begin in project status `Discovery`.
- Project discovery begins in discovery stage `Brainstorming`.
- Every DevCraft feature starts with `spec.md`.
- Features may only move to the next state when the operator explicitly asks.
- Features may only move to the next implementation phase when the operator explicitly asks.
- Code is only allowed while the active feature state is `Implementation`.
- Context files must be updated as part of completed work.
