# GitHub Issue Feature Storage

## Purpose

Use this feature storage mode when DevCraft feature artifacts should live on a GitHub issue instead of inside the project repository.

## Repository Shape

The project repository keeps only lightweight DevCraft state:

- `.devcraft/configure.json` - project DevCraft configuration.
- `.devcraft/features.json` - feature index with feature name, slug, storage mode, and GitHub issue link.

Feature artifacts such as `spec.md`, `research.md`, `tasks.md`, `analysis.md`, `issues.md`, and `results.md` are stored through the GitHub issue workflow.

## Start A Feature

1. Ask the operator for the GitHub issue that represents the feature.
2. If the operator does not already have an issue, ask them to create or choose one in GitHub.
3. Fetch the issue details from GitHub.
4. Record the issue number, issue title, URL, feature name, and feature slug in `.devcraft/features.json`.
5. Use the GitHub issue as the source of truth for feature identity and external tracking.

## Authentication

1. Ask the operator to authenticate with GitHub in the browser or through approved GitHub tooling when needed.
2. Obtain the access token from the authenticated browser/session flow, GitHub CLI, or approved GitHub tooling.
3. Do not store the token in the repository.
4. Use the token only for the current GitHub operation unless DevCraft later has an approved secure credential store.
5. If authentication fails or expires, ask the operator to re-authenticate.

## Create A Feature Artifact

1. Generate the DevCraft Markdown artifact locally in memory or a temporary working file.
2. Name the artifact by its DevCraft role, such as `spec.md`, `research.md`, `tasks.md`, `analysis.md`, `issues.md`, or `results.md`.
3. Attach, upload, or otherwise publish the Markdown artifact using the GitHub issue storage mechanism DevCraft supports.
4. Add or update a GitHub issue comment noting which artifact was created or updated.
5. Update `.devcraft/features.json` with the artifact metadata when available.

## Edit A Feature Artifact

1. Retrieve the current artifact content from the GitHub issue.
2. Edit the Markdown content according to the current DevCraft feature state.
3. Upload or publish the updated content according to the GitHub storage behavior DevCraft supports.
4. Add a GitHub issue comment summarizing the update.
5. Update `.devcraft/features.json` with the newest artifact metadata or version reference.

## Feature State Rules

- DevCraft state gates still apply.
- Do not write implementation code unless the feature state is `Implementation`.
- Do not move a feature state unless the operator explicitly asks.
- Keep the GitHub issue and `.devcraft/features.json` aligned.
- Treat the GitHub-backed artifact as the source of truth for each feature artifact in this mode.

## Failure Handling

- If GitHub cannot be reached, stop and report the issue.
- If the issue cannot be found, ask the operator for the correct issue.
- If artifact upload or publication fails, do not claim the artifact was saved.
- If local temporary files are used, remove them after upload when safe.
