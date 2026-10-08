# Skill: Create Library Installer

## Intent

Create DevCraft documentation-library installer scripts and a repository-local `catalog.json` so a library repository can install its Markdown resources into a user's profile-level DevCraft folder and merge the library catalog with `devcraft merge`.

## Triggers

- The operator asks to create installers for a DevCraft library repository.
- A repository contains reusable DevCraft skills, standards, architectures, project types, or templates.
- A library repository needs one-line install commands for macOS, Linux, and Windows.
- A repository needs a `catalog.json` that can be merged into profile `configure.json`.

## Inputs

- The library repository root.
- The public repository owner, repository name, and default branch for one-line install commands.
- The profile library folders to support: `skills`, `standards`, `architectures`, `project-types`, and optionally `templates`.
- The existing DevCraft library installer pattern, such as the one used by `DevCraft.OpenSource`.
- The installed DevCraft CLI command, which must support `devcraft merge <file>`.

## Installer Requirements

Create two installer scripts in the repository root:

- `install.sh` for macOS and Linux.
- `install.ps1` for Windows PowerShell.

The scripts must support copy-and-paste install commands:

- macOS and Linux: `/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/<OWNER>/<REPO>/<BRANCH>/install.sh)"`
- Windows: `irm https://raw.githubusercontent.com/<OWNER>/<REPO>/<BRANCH>/install.ps1 | iex`

Each installer must:

1. Confirm DevCraft is installed by finding profile `configure.json` and the `devcraft` binary.
2. Download the repository archive or accept a local source override for testing.
3. Copy Markdown files from supported library folders into the same relative folders under the profile-level DevCraft folder.
4. Back up an existing profile file before replacing it when the incoming file differs.
5. Leave unchanged files alone.
6. Run `devcraft merge <source>/catalog.json` after files are copied.
7. Fail clearly if `catalog.json` is missing or `devcraft merge` fails.
8. Avoid overwriting `soul.md`.

The scripts should support these environment overrides:

- `DEVCRAFT_HOME`: profile-level DevCraft folder. Default is `~/.DevCraft` on macOS/Linux and `$HOME\.DevCraft` on Windows.
- `DEVCRAFT_SOURCE`: archive URL, local archive, or local folder. Use this for forks and tests.

## Catalog Requirements

Create or refresh a repository-local `catalog.json`.

The catalog must include installable Markdown files from:

- `skills` as `Skills`.
- `standards` as `Standards`.
- `architectures` as `Architectures`.
- `project-types` as `ProjectTypes`.

Each entry must include:

- `Slug`: lowercase kebab-case and unique within its section.
- `Name`: readable document name.
- `Description`: document `## Purpose`, `## Intent`, or first meaningful paragraph.
- `Path`: repository-relative path to the Markdown file.

Do not catalog:

- `README.md`.
- Files whose names start with `_`.
- Hidden files.
- Non-Markdown files.
- `.devcraft` workflow files.

Templates may be copied by installers when a `templates` folder exists, but do not include templates in the merge catalog unless DevCraft merge supports template entries.

## Workflow

1. Inspect the repository root and identify supported library folders.
2. Read the Markdown files that should be cataloged.
3. Generate or refresh `catalog.json` from the current repository contents.
4. Create `install.sh` using the DevCraft library installer pattern.
5. Create `install.ps1` using the DevCraft library installer pattern.
6. Update `README.md` with the macOS/Linux and Windows one-line install commands, including safe inspect-before-running alternatives.
7. Validate the shell script syntax where possible.
8. Validate that every catalog entry points to an existing Markdown file.
9. If tests exist for catalog or installer behavior, update or add them.

## Output / Done Definition

- `install.sh` exists and installs the library on macOS and Linux.
- `install.ps1` exists and installs the library on Windows PowerShell.
- `catalog.json` exists and contains entries for every supported installable Markdown file.
- Every catalog entry points to a file that exists in the repository.
- The install scripts copy library Markdown files before calling `devcraft merge`.
- The install scripts replace changed profile files with backups and leave unchanged files alone.
- `README.md` documents the one-line install commands.
- The operator receives a concise summary of created or updated files and any validation that was run.
