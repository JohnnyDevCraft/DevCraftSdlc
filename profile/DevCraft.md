You must never write code when a project is using DevCraft, and you do not have a feature in the Implementation state.

# DevCraft Mode

## Purpose

DevCraft is a documentation-first workflow that keeps project context, feature intent, research, planning, analysis, implementation history, and issue handling explicit and auditable.

## When To Use

- The project should use the shared DevCraft operating model.
- The team wants durable project control files and feature artifacts.
- New work should move through explicit states before coding begins.

## Core Principles

- Documentation is part of the product.
- DevCraft project control files live under `.devcraft/`, not at the repo root.
- Features move through states controlled by the feature's selected operator mode.
- Code is only allowed while the active feature is in `Implementation`.
- Once a feature is `Complete`, further non-minor work must be handled through a new feature.

## Operator Modes

DevCraft features may run in one of two operator modes.

### User Operator Mode

User Operator Mode is the default DevCraft operating mode.

- The human operator controls every gate.
- The human operator decides when a feature moves from one state to the next.
- The human operator decides when a feature moves from one implementation phase to the next.
- The human operator must explicitly approve movement into `Implementation`.
- The human operator must explicitly allow implementation after reviewing and approving discovery, clarification, research, planning, and analysis artifacts.
- Agents may recommend state movement, phase movement, or implementation readiness, but they must not perform those transitions without human approval.

### Agent Operator Mode

Agent Operator Mode allows the agent to operate autonomously for the selected feature.

- The human operator may still participate in discovery and provide the original feature description, constraints, and goals.
- Once a feature is explicitly placed in Agent Operator Mode, the agent controls that feature's gates.
- The agent may decide when discovery, clarification, research, planning, and analysis are sufficient.
- The agent may move the feature into `Implementation` when it determines the feature is ready.
- The agent may decide which implementation phases to run and when to move between phases.
- The agent remains responsible for keeping all required DevCraft artifacts current as it advances the feature.

Agent Operator Mode does not remove the hard rule that code is only allowed while the active feature state is `Implementation`. It gives the agent authority to move the feature into `Implementation` when the feature's own artifacts justify that transition.


## Profile DevCraft Folder

The profile `.DevCraft/` folder is the installed DevCraft runtime and reusable knowledge library. It is separate from a project repository's `.devcraft/` workflow folder.

Profile root files:

- `.DevCraft/DevCraft.md` - the DevCraft operating rules.
- `.DevCraft/soul.md` - operator and assistant identity context.
- `.DevCraft/initialized.md` - startup handoff instructions for terminal AI clients.
- `.DevCraft/configure.json` - catalog of available reusable guidance and supported terminal clients.

Profile library folders:

- `.DevCraft/skills/` - reusable DevCraft skills.
- `.DevCraft/standards/` - reusable coding and engineering standards.
- `.DevCraft/architectures/` - reusable architecture guidance.
- `.DevCraft/templates/` - reusable creation templates, including `skill-template.md`.
- `.DevCraft/project-types/` - reusable project type definitions.

`configure.json` is the index for the profile library. Entries use lowercase kebab-case slugs so DevCraft commands can list, import, export, and reference exact items. The configuration also includes supported terminal clients such as Codex, Claude Code, and GitHub Copilot.

When DevCraft launches a terminal AI client, it should pass or reference `soul.md`, `initialized.md`, `configure.json`, and this `DevCraft.md` file so the client knows who it is working for, what resources are available, and how to operate in DevCraft mode.

## DevCraft Control Folder

The hidden `.devcraft/` folder is the source of truth for DevCraft workflow artifacts.

Default project control files:

- `.devcraft/AGENT.md`
- `.devcraft/README.md`
- `.devcraft/GOV.md`
- `.devcraft/MODEL.md`
- `.devcraft/THEME.md`
- `.devcraft/ARCH.md`
- `.devcraft/DISCOVERY.md`

Feature work lives under:

- `.devcraft/features/<id>-<FeatureNamePascalCased>/spec.md`
- `.devcraft/features/<id>-<FeatureNamePascalCased>/research.md`
- `.devcraft/features/<id>-<FeatureNamePascalCased>/tasks.md`
- `.devcraft/features/<id>-<FeatureNamePascalCased>/analysis.md`
- `.devcraft/features/<id>-<FeatureNamePascalCased>/issues.md`
- `.devcraft/features/<id>-<FeatureNamePascalCased>/results.md`

Only create the feature files that are needed for the feature's current state, except `spec.md`, which always exists first.

## Project Status

Valid DevCraft project statuses are:

- `Discovery`
- `Active`
- `Maintenance`

Project status rules:

- New DevCraft projects start in `Discovery`.
- While the project is in `Discovery`, work focuses on defining the product and its MVP.
- Once project discovery is complete, move the discovery document to `Complete` and move the project to `Active`.
- `Maintenance` is for projects that are primarily receiving support, fixes, and smaller follow-on work.
- Do not change project status unless the operator explicitly asks.

## Project Discovery Stages

The project discovery lifecycle is tracked in `.devcraft/DISCOVERY.md`.

Valid discovery stages are:

- `Brainstorming`
- `Architecture Planning`
- `Model Design`
- `Brand and Theming`
- `Governance Design`
- `Complete`

Discovery stage rules:

- New DevCraft projects start in `Brainstorming`.
- Do not move to the next discovery stage unless the operator explicitly asks.
- Discovery work stays in `.devcraft` until the project discovery lifecycle is `Complete`.
- Feature design begins after the project discovery lifecycle is `Complete` and the project status is `Active`.

## Feature States

Valid DevCraft feature states are:

- `Discovery`
- `Clarification`
- `Research`
- `Planning`
- `Analysis`
- `Implementation`
- `Complete`

State rules:

- In User Operator Mode, do not change a feature state unless the human operator explicitly asks.
- In User Operator Mode, do not move a feature to the next phase unless the human operator explicitly asks.
- In Agent Operator Mode, the agent may change feature state and move between phases when the feature artifacts support that transition.
- Do not write code unless the feature state is `Implementation`.
- Do not write code against a `Complete` feature. Open a new feature instead.

## State Responsibilities

## Project Discovery Responsibilities

### Brainstorming

- Work with the operator to define the purpose of the project.
- Identify the problems the software should solve.
- Define actors, requirements, use cases, MVP goals, and candidate features.
- Store these findings in `.devcraft/DISCOVERY.md`.
- When operator decisions are needed, use the shared DevCraft Discovery Questions skill and its required numbered question, contextual explanation, uppercase option, and final custom-option format.

### Architecture Planning

- Design the languages, frameworks, tools, and architecture approach for the solution.
- Record the resulting direction in `.devcraft/DISCOVERY.md`, `.devcraft/ARCH.md`, and `.devcraft/AGENT.md` as appropriate.

### Model Design

- Break down the data structures that matter to the application.
- Record the important entities, relationships, and constraints in `.devcraft/DISCOVERY.md` and `.devcraft/MODEL.md`.

### Brand and Theming

- Theme exploration happens in Theme Maker.
- Exported theme assets such as logos and images may later be converted and refined by the operator outside Codex.
- Once the theme folder is added to the project, use it to update `.devcraft/THEME.md`.

### Governance Design

- Define the compliance, operational, security, and policy constraints the project must respect.
- Record them in `.devcraft/DISCOVERY.md` and `.devcraft/GOV.md`.

### Complete

- Project discovery is closed.
- The project can move to `Active`.
- Feature design may begin after the operator explicitly advances the project.

### Discovery

- Create `spec.md`.
- Capture the feature type, goals, overview, current state, target state, requirements, and non-goals.
- Define the target outcome for the feature.
- Record how relevant standards from profile `.DevCraft` should apply to the feature.
- Do not create implementation tasks yet unless the operator explicitly wants early placeholders.

### Clarification

- Review `spec.md`.
- Surface one question at a time.
- After each answer, immediately update `spec.md`.
- Continue until the request is fully described and materially free of gaps.
- Recommendations are allowed, but the operator decides.
- Use the shared DevCraft Discovery Questions skill and its required numbered question, contextual explanation, uppercase option, and final custom-option format.

### Research

- Use `spec.md` to guide research.
- Research APIs, current codebase state, existing constraints, and implementation best practices.
- Save findings in `research.md`.
- Record concrete sources, repo evidence, risks, and recommended direction.

### Planning

- Use `spec.md` and `research.md`.
- Create `tasks.md`.
- Break the work into phases.
- Add explicit tasks for each phase.
- Separate implementation tasks from test/validation tasks.
- Make test tasks concrete and named.

### Analysis

- Compare `tasks.md` against `spec.md` and `research.md`.
- Create `analysis.md`.
- Record inconsistencies, gaps, risks, and questionable assumptions.
- Surface issues one at a time with recommendations.
- After the operator chooses a resolution, record it in `analysis.md`.
- If the operator gives a custom solution and it introduces a new issue, record that issue in `analysis.md`, surface it next, then resume the queue.
- After all issues are resolved, update `spec.md` and `tasks.md` to reflect the chosen solutions.

### Implementation

- Code is allowed only in this state.
- In User Operator Mode, implement one phase at a time unless the human operator explicitly authorizes multiple phases together or names specific phases to bundle.
- In Agent Operator Mode, the agent may choose the implementation phase order and bundling when the feature artifacts support that decision.
- As code changes are made, record them in `results.md` with what changed and why.
- After a phase is implemented, operator review may surface implementation issues.
- Record each such issue in `issues.md` using an ID you create.
- Resolve the issue.
- Record the resolution in `results.md` and reference the issue ID.

### Complete

- The feature is closed.
- No further coding is allowed against that feature.
- New non-minor work must be handled through a new feature request with its own artifacts.

## Status-Gated Implementation Rule (Hard Gate)

Implementation permission is controlled by feature state.

- `Discovery`, `Clarification`, `Research`, `Planning`, and `Analysis` are non-coding states.
- `Implementation` is the only coding state.
- `Complete` is a closed state.

If the active feature is not in `Implementation`, agents must not:

- Create, edit, move, or delete application code
- Add migrations, scaffolds, or generators
- Run commands whose primary purpose is to mutate repo implementation state

## Branching Models

DevCraft supports two branching models.

### Simple Branching

- Use one master branch for active development.
- Implementation-state work happens directly on the master branch.

### Advanced Branching

- Create a feature branch from the master branch for each implementation feature.
- Use the naming convention:
  - `[username]/[bug|feature|task|enabler]/[ticket-id]-[ai_generated_description_up_to_55_chars]`

## Project Setup Flow

1. Confirm the project will operate in `DevCraft` mode.
2. Create the project root `AGENT.md` immediately and record that the active mode is `DevCraft`.
3. Create `.devcraft/`.
4. Generate the DevCraft control files inside `.devcraft/` using the templates in [`./templates`](./templates).
5. Ascertain project purpose, users, rules, security concerns, languages, frameworks, architecture, work-item handling, collaboration model, branching model, and master branch.
6. Set the project status to `Discovery` and the discovery stage to `Brainstorming`.
7. Fill the `.devcraft` control files with the best known information before implementation begins.
8. If a shared common design applies, load [Design.md](./Design.md) and the relevant file in `./designs`.

## Existing Project Flow

1. Read the project's `AGENT.md`.
2. Read the DevCraft control files in `.devcraft/`.
3. Identify the current project status and discovery stage.
4. Identify the active feature and current feature state when feature work has begun.
5. Identify the relevant standards, architecture, tools, and common designs.
6. Keep `.devcraft` current as work progresses.

## Feature Flow

1. Create `.devcraft/features/<id>-<FeatureNamePascalCased>/`.
2. Create `spec.md` first from the spec template.
3. Set the feature type:
   - `Feature`
   - `Bug`
   - `Enabler`
   - `Task`
4. Set the feature state explicitly.
5. Move through the states only when the operator asks.
6. Create `research.md` during `Research` when needed.
7. Create `tasks.md` during `Planning`.
8. Create `analysis.md` during `Analysis`.
9. Create `issues.md` when implementation review issues appear or when post-phase fixes are needed before `Complete`.
10. Create and maintain `results.md` during `Implementation`.
11. In `spec.md`, document how relevant profile `.DevCraft/standards` entries apply to the feature.
12. Keep each artifact aligned to its purpose instead of overloading `spec.md` with every workflow concern.

## Required Project Documents

These are the default DevCraft project control files for a new project:

- `.devcraft/AGENT.md`
- `.devcraft/README.md`
- `.devcraft/GOV.md`
- `.devcraft/MODEL.md`
- `.devcraft/THEME.md`
- `.devcraft/ARCH.md`
- `.devcraft/DISCOVERY.md`

## Template Files

Reusable creation templates live in the profile `.DevCraft/templates/` folder and are indexed by `configure.json`. The current required skill template is:

- `.DevCraft/templates/skill-template.md`

Project-control templates may be added to this folder later and should be indexed in `configure.json` when they become user-selectable resources.

## AI Responsibilities In DevCraft

- Ask for the active mode when a new project begins.
- Create the project root `AGENT.md` immediately and record `DevCraft` as the active mode.
- Create `.devcraft/` and maintain the DevCraft control files there.
- Track project status in `.devcraft/AGENT.md` as `Discovery`, `Active`, or `Maintenance`.
- Track project discovery stage in `.devcraft/DISCOVERY.md`.
- Create and maintain typed feature folders under `.devcraft/features/`.
- Start new feature work in `Discovery`.
- Do not start feature design until project discovery is complete and the project is `Active`, unless the operator explicitly chooses otherwise.
- During `Clarification`, ask one question at a time and update `spec.md` after each answer.
- During `Research`, gather concrete internal and external evidence and save it to `research.md`.
- During `Planning`, build phased tasks in `tasks.md`.
- During `Analysis`, compare artifacts, record issues in `analysis.md`, and surface them one at a time.
- During `Implementation`, apply only the operator-approved phases, record changes in `results.md`, and track implementation review issues in `issues.md`.
- Keep context files current after each completed request.
- Run required validation before claiming implementation is complete.

## Implementation Rules

- Default mode is documentation and analysis, not coding.
- `spec.md` is always the starting artifact for a feature.
- `spec.md` must include how shared standards from profile `.DevCraft` apply to the feature.
- `research.md` is the source of truth for gathered evidence, investigated options, current-code findings, external-source findings, risks, and recommended direction discovered during Research.
- `tasks.md` is the source of truth for implementation phases and tasks.
- `analysis.md` is the source of truth for plan-vs-spec-vs-research gap analysis.
- `results.md` is the implementation change log.
- `issues.md` tracks implementation issues discovered after or during phase review.
- Test tasks must be explicit and concrete.
- In User Operator Mode, the human operator controls state movement and phase movement.
- In Agent Operator Mode, the agent controls state movement and phase movement for that feature.
- Once a feature reaches `Complete`, open a new feature for further non-minor work.
- Validation still requires build success, relevant automated tests, Playwright when applicable, persistence-after-reload checks when applicable, and screenshot review for UI-affecting work.
