# Skill: Create Project Type

## Intent

Help the operator design a reusable DevCraft project type through a focused back-and-forth conversation, then produce a completed project type document from the project type template.

## Triggers

- The operator asks to create, design, define, or refine a DevCraft project type.
- DevCraft needs a reusable project type before it can scaffold or guide a class of projects.
- A new project pattern should become repeatable across repositories.

## Inputs

- The project type name or working name.
- The kind of software or repository the project type creates.
- Any required standards, architectures, skills, frameworks, commands, or project layout rules.
- The profile template at `templates/project-type-template.md`.

## Conversation Workflow

1. Start by asking what kind of project the operator wants DevCraft to create or recognize.
2. Ask what problem that project type solves and what a successful generated project should contain.
3. Ask which standards, architectures, and skills should be associated with the project type.
4. Ask what setup steps DevCraft should follow, including project names, project kinds, physical paths, and optional solution folder paths.
5. Repeat the proposed project type back to the operator as a short structured summary before writing it.
6. Ask the operator to approve, revise, or add missing details.
7. After approval, create the project type document from `templates/project-type-template.md` and write it to the `project-types` folder.

## Communication Rules

- Ask one focused group of questions at a time.
- Use clear options when the operator is choosing between likely project patterns.
- Prefer the operator's words for project purpose, naming, and setup language.
- Do not invent mandatory standards, architectures, or skills when the operator has not chosen them.
- Confirm the proposed project type before creating or updating files.
- Keep the conversation practical: every answer should help fill in the project type template.

## Output / Done Definition

- A project type Markdown file exists under `project-types`.
- The file was created from `templates/project-type-template.md`.
- The file has a clear project type name, description, default standards, default architectures, and setup guidance.
- The setup guidance is specific enough for DevCraft to scaffold or guide that project type later.
- `configure.json` is updated so the new project type appears with a stable slug, description, and path.

