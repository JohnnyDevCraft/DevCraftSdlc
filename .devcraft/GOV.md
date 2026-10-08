# Governance

## Purpose

Track the rules, controls, approvals, and operational boundaries that govern DevCraft.

## Product Governance

- Who can use the system: John initially; future developers and AI agents after the repository matures
- Who can administer the system: The operator
- Approval requirements: The operator controls project discovery movement, feature state movement, and implementation phase movement
- Business rules: DevCraft must preserve documentation-first workflow control

## Security Governance

- Sensitive data concerns: DevCraft project files may contain project-specific context and should avoid unnecessary secrets
- Authentication requirements: None identified during brainstorming
- Authorization requirements: Repository and workflow mutation must follow operator approval
- Audit and logging expectations: Feature implementation must record results in `results.md`

## AI Governance

- What AI may do directly: Maintain DevCraft control files during authorized discovery and planning
- What AI may draft only: Specs, research, tasks, analysis, and implementation plans before approval
- What requires human approval: State transitions, implementation start, implementation phase movement, and rule changes
- What must never be automated: Bypassing DevCraft gates or silently changing feature/project status
- Personalization data: Soul file content is operator-provided identity and working-context guidance for the AI. It exists to tell the AI who it is and who it is working for.

## Engineering Governance

- Branching model: Undecided
- Release or deployment controls: Undecided
- Testing requirements: One responsibility per automated test; validation must prove the user-visible outcome
- Documentation requirements: DevCraft artifacts are part of the product and must stay current
- Installation requirements: Install scripts must be explicit, reviewable, and limited to copying required DevCraft binaries, Markdown structure files, and approved plugin/extension assets into the user's profile `.DevCraft` folder unless the operator approves additional behavior.
- Plugin governance: Plugins and related extensions loaded from the profile `.DevCraft` folder need a later trust and permission model before execution behavior is implemented.
- Soul governance: Keep the soul-file workflow simple. The operator authors the soul file, and DevCraft uses it as local AI identity and operator-context guidance.

## Operational Governance

- Queue or background process requirements: None identified yet
- Failure handling rules: Console tooling and install scripts should surface failures clearly and avoid destructive defaults
- Support or escalation notes: Unresolved workflow decisions return to the operator

## Notes

This repository is intentionally being built with DevCraft itself. That means the console application may not be created until a feature reaches `Implementation`.
