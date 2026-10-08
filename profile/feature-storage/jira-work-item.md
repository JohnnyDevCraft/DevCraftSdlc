# JIRA Work Item Feature Storage

## Purpose

Use this feature storage mode when DevCraft feature artifacts should live on a JIRA issue instead of inside the project repository.

## Repository Shape

The project repository keeps only lightweight DevCraft state:

- `.devcraft/configure.json` - project DevCraft configuration.
- `.devcraft/features.json` - feature index with feature name, slug, storage mode, and JIRA issue link.

Feature artifacts such as `spec.md`, `research.md`, `tasks.md`, `analysis.md`, `issues.md`, and `results.md` are stored as attachments on the JIRA issue.

## Start A Feature

1. Ask the operator for the JIRA issue that represents the feature.
2. If the operator does not already have an issue, ask them to create or choose one in JIRA.
3. Fetch the issue details from JIRA.
4. Record the issue key, issue title, URL, feature name, and feature slug in `.devcraft/features.json`.
5. Use the JIRA issue as the source of truth for feature identity and external tracking.

## Authentication

1. Ask the operator to authenticate with JIRA in the browser when needed.
2. Obtain the access token from the authenticated browser/session flow or approved JIRA tooling.
3. Do not store the token in the repository.
4. Use the token only for the current JIRA operation unless DevCraft later has an approved secure credential store.
5. If authentication fails or expires, ask the operator to re-authenticate.

## Create A Feature Artifact

1. Generate the DevCraft Markdown artifact locally in memory or a temporary working file.
2. Name the artifact by its DevCraft role, such as `spec.md`, `research.md`, `tasks.md`, `analysis.md`, `issues.md`, or `results.md`.
3. Attach the Markdown file to the JIRA issue.
4. Add or update a JIRA comment noting which artifact was created or updated.
5. Update `.devcraft/features.json` with the artifact attachment metadata when available.

## Edit A Feature Artifact

1. Download the current attachment for the artifact from the JIRA issue.
2. Edit the Markdown content according to the current DevCraft feature state.
3. Upload the updated content as a new attachment or replacement according to the JIRA API behavior DevCraft supports.
4. Add a JIRA comment summarizing the update.
5. Update `.devcraft/features.json` with the newest attachment metadata or version reference.

## Feature State Rules

- DevCraft state gates still apply.
- Do not write implementation code unless the feature state is `Implementation`.
- Do not move a feature state unless the operator explicitly asks.
- Keep the JIRA issue and `.devcraft/features.json` aligned.
- Treat the JIRA attachment as the source of truth for each feature artifact in this mode.

## Failure Handling

- If JIRA cannot be reached, stop and report the issue.
- If the issue cannot be found, ask the operator for the correct issue.
- If an attachment upload fails, do not claim the artifact was saved.
- If local temporary files are used, remove them after upload when safe.
