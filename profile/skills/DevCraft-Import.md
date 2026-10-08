# Skill: DevCraft Import

## Intent

Import reusable DevCraft documentation from a local documentation repository into the profile-level DevCraft folder, then update the profile catalog in `configure.json`.

## Triggers

- The operator chooses Configure DevCraft > Import Settings.
- The operator provides a local folder containing reusable DevCraft documentation.
- DevCraft needs to merge standards, architectures, project types, or skills from another repository into the local profile.

## Inputs

- The local import source folder.
- The profile-level DevCraft folder.
- The profile `configure.json` file.
- Source folders named `standards`, `architectures`, `project-types`, and `skills`.

## Import Mapping

- Source `standards` Markdown files go to profile `standards`.
- Source `architectures` Markdown files go to profile `architectures`.
- Source `project-types` Markdown files go to profile `project-types`.
- Source `skills` Markdown files go to profile `skills`.

## Catalog Rules

Each imported Markdown file must have an entry in the matching `configure.json` section:

- Standards go into `Standards`.
- Architectures go into `Architectures`.
- Project types go into `ProjectTypes`.
- Skills go into `Skills`.

Each catalog entry must include:

- `Slug`: a unique lowercase kebab-case identifier generated from the document name.
- `Name`: the readable document name.
- `Description`: the document purpose, intent, or first meaningful paragraph.
- `Path`: the profile-relative path to the imported file.

If an imported file targets the same profile-relative path as an existing file, replace the existing file with the imported file. The imported repository is the source of truth for that file during this import.

If a catalog entry already exists for the same profile-relative path or slug, update that existing entry with the imported file's current name, description, slug, and path instead of creating a duplicate.

If the imported item is genuinely new and its generated slug conflicts with an unrelated existing item, add a numeric suffix such as `-2`, `-3`, or the next available number.

## Workflow

1. Verify the import source folder exists.
2. Inspect only the supported source folders: `standards`, `architectures`, `project-types`, and `skills`.
3. Ignore non-Markdown files, `README.md`, and underscore-prefixed template files unless the operator explicitly asks to import them.
4. Copy each valid Markdown file into the matching profile-level DevCraft folder.
5. Replace any existing profile-level file at the same target path with the imported file without asking for additional approval.
6. Generate or refresh the matching catalog entries in profile `configure.json`.
7. Report what was imported, what was replaced, what was skipped, and whether any names or slugs needed collision handling.

## Output / Done Definition

- Imported Markdown files exist in the correct profile-level DevCraft folders.
- Profile `configure.json` includes every imported item in the correct catalog section.
- Every imported item has a stable slug, readable name, description, and profile-relative path.
- Existing profile files at matching target paths are replaced by the imported versions.
- Existing catalog entries for matching paths or slugs are refreshed rather than duplicated.
- The operator receives a concise import summary with imported, replaced, skipped, and collision-handled files.
