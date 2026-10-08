# Skill: Operator Questions

## Intent

Present material questions to the operator of a project or feature in a consistent, decision-ready format outside formal DevCraft discovery and clarification sessions.

## Triggers

- Codex needs the operator to choose between materially different project or feature directions.
- A missing operator decision prevents safe progress.
- The operator asks to be presented with options.

Do not use this skill for simple factual questions, credentials, confirmations with only yes/no meaning, or information that can be safely discovered from the workspace.

## Required Question Format

```text
1. Full Question Written as a Complete Question?

   Context: Explain what is being decided, why it matters now, relevant terminology, and the consequences for the project or feature.

   A. Option 1 — State the consequence or tradeoff. Mark as Recommended when applicable and explain why.
   B. Option 2 — State the consequence or tradeoff.
   C. Option 3 — State the consequence or tradeoff.
   D. Custom Option — The operator may provide a different direction.
```

Rules:

- Number every question and write it as a complete question.
- Put indented context between the question and its options.
- Use uppercase option letters.
- Always make the final choice `Custom Option`.
- If more predefined choices are necessary, continue lettering and place `Custom Option` last.
- State the practical consequence or tradeoff for every predefined option.
- Put the recommended option first when there is a clear recommendation and label it `Recommended`.
- Ask all currently known related questions in one batch unless the operator requests one at a time.
- End with a compact response example such as `1A, 2C, 3D - <custom answer>`.

## Workflow

1. Verify the answer cannot be safely inferred or discovered.
2. Gather the minimum project or feature context needed to explain the decision accurately.
3. Present the question batch using the required format.
4. Wait for the operator's selections.
5. Apply or record the decisions only within the authority and workflow state already granted.
6. If the decision belongs in DevCraft source-of-truth artifacts, update those artifacts immediately.

## Output / Done Definition

- The operator receives complete questions with context, consequences, uppercase options, and a custom option.
- The response syntax is clear.
- Selected decisions are applied or recorded without silently expanding scope or advancing workflow state.
