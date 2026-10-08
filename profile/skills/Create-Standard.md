# Skill: Create Standard

## Intent

Help the operator design a reusable DevCraft standard through a focused back-and-forth conversation, then produce a completed standard document from the standard template.

## Triggers

- The operator asks to create, design, define, or refine a DevCraft standard.
- DevCraft needs repeatable coding, documentation, testing, design, governance, or operational guidance.
- A team or project convention should become available across project types and repositories.

## Inputs

- The standard name or working name.
- The language, framework, artifact, or practice the standard governs.
- The baseline authority or existing convention the standard should start from.
- Required rules, preferences, examples, avoidances, and testing expectations.
- The profile template at `templates/standard-template.md`.

## Conversation Workflow

1. Ask what practice, language, framework, artifact, or workflow the standard should govern.
2. Ask what baseline source or existing convention should anchor the standard.
3. Ask which rules are mandatory and which are preferences.
4. Ask what examples, testing expectations, and avoidances should be included.
5. Repeat the proposed standard back to the operator as a short structured summary before writing it.
6. Ask the operator to approve, revise, or add missing details.
7. After approval, create the standard document from `templates/standard-template.md` and write it to the `standards` folder.

## Communication Rules

- Ask one focused group of questions at a time.
- Use clear options when the operator is choosing between common convention patterns.
- Prefer the operator's words for rules, exceptions, and avoidances.
- Do not invent mandatory rules, external authorities, or compliance requirements when the operator has not chosen them.
- Confirm the proposed standard before creating or updating files.
- Keep the conversation practical: every answer should help fill in the standard template.

## Output / Done Definition

- A standard Markdown file exists under `standards`.
- The file was created from `templates/standard-template.md`.
- The file has a clear standard name, purpose, baseline, principles, guidance sections, testing expectations, avoid list, and notes.
- `configure.json` is updated so the new standard appears with a stable slug, description, and path.

