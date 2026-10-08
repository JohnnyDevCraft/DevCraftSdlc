# Skill: Create Architecture

## Intent

Help the operator design a reusable DevCraft architecture through a focused back-and-forth conversation, then produce a completed architecture document from the architecture template.

## Triggers

- The operator asks to create, design, define, or refine a DevCraft architecture.
- DevCraft needs reusable architecture guidance before planning or generating a project.
- A recurring structural pattern should become available across project types and repositories.

## Inputs

- The architecture name or working name.
- The kind of system, application, service, or repository the architecture supports.
- The constraints, tradeoffs, boundaries, integrations, and testing expectations the architecture should describe.
- The profile template at `templates/architecture-template.md`.

## Conversation Workflow

1. Ask what kind of system or project the architecture should guide.
2. Ask what problem the architecture solves and when DevCraft should recommend it.
3. Ask for the core principles, major components, boundaries, data flow, and integration expectations.
4. Ask what the architecture should avoid or warn against.
5. Ask what testing, deployment, and operational concerns belong in the architecture.
6. Repeat the proposed architecture back to the operator as a short structured summary before writing it.
7. Ask the operator to approve, revise, or add missing details.
8. After approval, create the architecture document from `templates/architecture-template.md` and write it to the `architectures` folder.

## Communication Rules

- Ask one focused group of questions at a time.
- Use clear options when the operator is choosing between common architecture patterns.
- Prefer the operator's words for the architecture purpose, names, boundaries, and tradeoffs.
- Do not invent mandatory technologies, hosting platforms, or patterns when the operator has not chosen them.
- Confirm the proposed architecture before creating or updating files.
- Keep the conversation practical: every answer should help fill in the architecture template.

## Output / Done Definition

- An architecture Markdown file exists under `architectures`.
- The file was created from `templates/architecture-template.md`.
- The file has a clear architecture name, purpose, usage guidance, principles, structure, boundaries, data and integration notes, testing guidance, and avoid list.
- `configure.json` is updated so the new architecture appears with a stable slug, description, and path.

