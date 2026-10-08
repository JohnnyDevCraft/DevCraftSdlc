# Repo Central Feature Storage

## Purpose

Store DevCraft feature artifacts inside the repository DevCraft folder. This is the current local-repository behavior.

## Storage Location

Repo Central stores feature data inside the repository DevCraft folder:

```text
.devcraft/
  configure.json
  features/
```

The repository owns its feature documentation. Feature files such as `spec.md`, `research.md`, `tasks.md`, `analysis.md`, `issues.md`, and `results.md` are created under the repository DevCraft feature structure.

## Start a Feature

1. Read `.devcraft/configure.json` from the repository DevCraft folder.
2. Create a feature entry in the repository feature index.
3. Create the feature folder under `.devcraft/features`.
4. Create the standard DevCraft feature artifacts in that folder.

## Edit Feature Artifacts

When DevCraft edits a feature artifact in Repo Central mode, it must:

1. Resolve the feature by slug in the repository feature index.
2. Read or create the artifact in `.devcraft/features`.
3. Save edits back to the repository DevCraft folder.
4. Keep the feature index aligned with the files on disk.

## State Rules

- Feature documentation belongs to the repository.
- This mode is best when the team wants feature documentation versioned with the code.
- The repository DevCraft folder remains the source of truth for the project feature workflow.

