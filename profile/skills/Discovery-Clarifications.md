# Skill: DevCraft Discovery Questions

## Intent

Drive clear operator decisions during both project-level DevCraft discovery and feature-level DevCraft discovery or clarification. Present every material decision as a fully written numbered question, followed by explanatory context and lettered options in the operator's required format.

## Triggers

- A DevCraft project is in a project discovery stage and needs operator decisions.
- A DevCraft feature is in `Discovery` or `Clarification` and needs operator decisions.
- The operator asks for discovery questions or clarification questions.
- Project or feature artifacts contain material ambiguities that should not be decided automatically.

## Scope Routing

### Project-Level Discovery

Use project-level discovery when the active decision concerns the product, MVP, actors, solution boundaries, architecture, data model, theming, governance, delivery, or other cross-feature direction.

Read the relevant project artifacts:

- `.devcraft/AGENT.md`
- `.devcraft/DISCOVERY.md`
- `.devcraft/ARCH.md`
- `.devcraft/MODEL.md`
- `.devcraft/THEME.md`
- `.devcraft/GOV.md`

Record each answer in the affected project artifact immediately. Do not create a feature merely to hold project-discovery decisions.

### Feature-Level Discovery And Clarification

Use feature-level discovery when the decision concerns one active feature's behavior, scope, user experience, compatibility, security, validation, or acceptance criteria.

Read:

- The active `.devcraft/features/<id>-<FeatureNamePascalCased>/spec.md`
- Relevant project-level `.devcraft/` artifacts

Record each answer in `spec.md`, including its clarification log, and align requirements, acceptance criteria, risks, and open questions.

## Required Question Format

Every question must use this structure:

```text
1. Full Question Written as a Complete Question?

   Context: Explain the concept, the current project or feature situation, why the decision matters, and the practical consequences. Define specialized terms and acronyms. Give the operator enough information to decide without needing a second explanation.

   A. Option 1 — State the practical consequence or tradeoff. Mark as Recommended when applicable and explain why it fits this project or feature.
   B. Option 2 — State the practical consequence or tradeoff.
   C. Option 3 — State the practical consequence or tradeoff.
   D. Custom Option — The operator may provide a different direction.
```

Format rules:

- Use an integer and a complete question for each decision.
- Put the context after the question and before the options.
- Indent the context and options beneath the question.
- Use uppercase option letters.
- Always include `D. Custom Option` even when only two predefined choices are needed.
- When more than three predefined choices are materially useful, continue with `D`, `E`, and so on, and make the final option `Custom Option`.
- Give every predefined option a consequence or tradeoff, not only a label.
- Put the recommended predefined option first when a recommendation is clear and label it `Recommended`.
- Explain the project-specific reason for the recommendation in the option or context.
- Do not compress the question into a heading followed by terse labels.

## Workflow

1. Determine whether the questions are project-level or feature-level.
2. Read the appropriate source-of-truth DevCraft artifacts.
3. Exclude questions already answered by the operator or artifacts.
4. Identify all currently known material decisions.
5. Present them in one numbered batch using the required format unless the operator explicitly asks for one question at a time.
6. End with a compact response example such as `1A, 2C, 3D - <custom answer>`.
7. After the operator answers, update the appropriate DevCraft artifacts immediately.
8. Record the selected option, resulting decision, affected requirements, and any remaining risk or follow-up.
9. If an answer creates a genuinely new material question, present a new batch containing only unresolved questions.
10. Do not change the project discovery stage or feature state unless the operator explicitly asks.

## Output / Done Definition

- Questions follow the required numbered/context/uppercase-option format.
- Every question includes a final custom option.
- Project-level answers are recorded in the appropriate `.devcraft/` project artifacts.
- Feature-level answers are recorded in the feature `spec.md` and clarification log.
- Resolved decisions and remaining unknowns are summarized.
- No application code is changed unless an active feature is explicitly in `Implementation`.

## Guardrails

- Preserve confirmed operator direction; never re-ask it as unresolved.
- Do not disguise an implementation preference as a product decision.
- Ask only decisions that materially affect the product, feature, architecture, risk, validation, or delivery.
- Do not optimize for brevity at the expense of decision quality.
- Recommendations are advisory; the operator decides.
