# Skill: Update Library Catalog

## Intent

Refresh a DevCraft documentation-library repository's `catalog.json` so it reflects the current skills, standards, architectures, and project types that can later be installed and merged into a profile-level DevCraft catalog.

## Triggers

- The operator adds, removes, renames, or edits reusable DevCraft documentation in a library repository.
- The operator asks to update, rebuild, regenerate, or refresh a library `catalog.json`.
- A library repository already has installers, but its catalog needs to reflect new Markdown files.
- The operator adds a new skill, standard, architecture, or project type and wants it included in future installs.

## Inputs

- The library repository root.
- Existing `catalog.json`, if present.
- Supported library folders:
  - `skills`
  - `standards`
  - `architectures`
  - `project-types`
- The Markdown files in those folders.

## Catalog Rules

The refreshed catalog must include:

- `Skills` for Markdown files in `skills`.
- `Standards` for Markdown files in `standards`.
- `Architectures` for Markdown files in `architectures`.
- `ProjectTypes` for Markdown files in `project-types`.

Each entry must include:

- `Slug`: lowercase kebab-case and stable when the document name has not changed.
- `Name`: readable document name.
- `Description`: document `## Purpose`, `## Intent`, or first meaningful paragraph.
- `Path`: repository-relative path to the Markdown file.

Skip:

- `README.md`.
- Files whose names start with `_`.
- Hidden files.
- Non-Markdown files.
- `.devcraft` workflow artifacts.
- Files outside the supported library folders.

If a file was removed from the repository, remove its catalog entry.

If a file was renamed, update the `Path`; keep the existing slug only when it still correctly represents the document name and does not conflict.

If a new item would duplicate an existing slug in the same section, add the next numeric suffix, such as `-2` or `-3`.

## Workflow

1. Confirm the current repository is a DevCraft documentation-library repository or ask the operator for the library root.
2. Read the existing `catalog.json` when it exists so stable slugs can be preserved where appropriate.
3. Scan only the supported library folders.
4. Build the catalog sections from the current Markdown files.
5. Preserve stable slugs for unchanged items when possible.
6. Remove entries for files that no longer exist.
7. Write the updated `catalog.json` with readable indentation.
8. Validate that every catalog entry's `Path` exists.
9. Report added, updated, removed, skipped, and unchanged entries.

## Output / Done Definition

- `catalog.json` reflects the current repository library files.
- Every catalog entry points to an existing Markdown file.
- Removed files no longer have catalog entries.
- New skills, standards, architectures, and project types have catalog entries.
- The catalog is ready for `devcraft merge catalog.json` after installer files are copied into the profile-level DevCraft folder.
- The operator receives a concise summary of catalog changes and validation results.
