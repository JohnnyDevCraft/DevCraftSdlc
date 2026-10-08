# Skill: Create Skill

## Intent

Help the operator design a reusable DevCraft skill through a focused back-and-forth conversation, then produce a completed skill document from the skill template.

## Triggers

- The operator asks to create, design, define, or refine a DevCraft skill.
- DevCraft needs repeatable instructions for a workflow, task type, artifact type, or decision process.
- A recurring way of working should become available across projects.

## Inputs

- The skill name or working name.
- The situations or operator phrasing that should trigger the skill.
- The information the AI needs from the operator, repository, or profile DevCraft folder.
- The workflow the AI should follow.
- The output or done definition for the skill.
- The profile template at `templates/skill-template.md`.

## Conversation Workflow

1. Ask what recurring DevCraft workflow or behavior the skill should capture.
2. Ask when the skill should be used, including explicit trigger phrases and project situations.
3. Ask what inputs the AI needs before it can use the skill safely.
4. Ask for the workflow steps the AI should follow.
5. Ask what output, files, decisions, or user-visible results prove the skill is complete.
6. Repeat the proposed skill back to the operator as a short structured summary before writing it.
7. Ask the operator to approve, revise, or add missing details.
8. After approval, create the skill document from `templates/skill-template.md` and write it to the `skills` folder.

## Communication Rules

- Ask one focused group of questions at a time.
- Use clear options when the operator is choosing between likely skill scopes.
- Prefer the operator's words for skill intent, trigger language, and done definitions.
- Do not invent required inputs, authority, or file changes when the operator has not chosen them.
- Confirm the proposed skill before creating or updating files.
- Keep the conversation practical: every answer should help fill in the skill template.

## Output / Done Definition

- A skill Markdown file exists under `skills`.
- The file was created from `templates/skill-template.md`.
- The file has a clear skill name, intent, triggers, inputs, workflow, and output or done definition.
- `configure.json` is updated so the new skill appears with a stable slug, description, and path.

