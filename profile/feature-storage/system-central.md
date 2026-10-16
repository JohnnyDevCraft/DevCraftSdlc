# System Central Feature Storage

## Purpose

Store DevCraft feature artifacts in the profile DevCraft folder so multiple repositories can be tracked from one central system-level feature store.

## Storage Location

System Central stores feature data under the profile DevCraft folder:

```text
.DevCraft/
  features/
    projects.json
    {feature-guid}/
      spec.md
      research.md
      tasks.md
      analysis.md
      issues.md
      results.md
```

The `projects.json` file is the system-level index of projects known to DevCraft, and the full feature index for every project and every storage type. Each project entry has:

- `id` - project key
- `name` - project name
- `slug` - project slug
- `repo-name` - repository name, from the `origin` remote URL (last path segment, without `.git`), or the working copy's folder name when there is no `origin`
- `repo-remote` - normalized `origin` URL (user info and scheme removed, host lowercased, trailing `/` and `.git` removed), or `null`
- `working-copies` - every checkout of the repository DevCraft has run in: `path`, `branch` (`null` on a detached HEAD or outside git), and `last-seen` (ISO 8601 UTC)
- `features` - array of features

Each feature entry has:

- `id` and `folder-name` - folder name {feature-guid}
- `feature-name` - feature name
- `slug` - feature slug
- `description` - feature short description
- `storage-type` - feature storage type
- `work-item-id` - the work item's ID in any tracker (free text such as `1234`, `PROJ-482`, `AB#1234`), or `null`
- `external-reference` - the work item's web URL (absolute `http` or `https`), or `null`
- `branch` - the git branch the feature is worked on, or `null`
- `status` - feature state

Files written by DevCraft beta 20 or earlier load unchanged: a `repo-location` value becomes the first working copy.

## Where Features Are Indexed

- `projects.json` indexes every feature of every storage type, and owns each feature's `status`.
- The repository `.devcraft/configure.json` `Features` list holds only `repo-central` features, so those travel with the repository. System Central, ADO, GitHub, and JIRA features are never committed to the repository. When no repo-central feature exists, `Features` is left out entirely.
- For a repo-central feature, both files are written: the repository owns the name, work item ID, URL, and branch; `projects.json` owns `status`.
- The repository `SelectedFeatureStorage` is the storage type given to new features. It does not move existing features.

Each time a feature command runs in a working copy, DevCraft:

1. Records this working copy (path, current branch, now) and refreshes `repo-name` and `repo-remote`. Other working copies are left as they are; DevCraft never removes one on its own.
2. Moves any non-repo-central entry it finds in `configure.json` into `projects.json` (added when missing; the central record wins when it exists), then rewrites `configure.json` without it and asks the operator to commit the change. Older branches heal themselves this way when they are checked out or merged.
3. Adds missing repo-central entries to `projects.json` and carries the central `status` back.

The project key and each feature folder name are randomly generated GUID values. The same project key is stored in the repository `.devcraft/configure.json`, which is committed, so every clone and worktree of the repository maps to the same project entry.

If a clone has a project key that `projects.json` does not know, but its normalized `origin` matches a known project's `repo-remote`, DevCraft asks "This clone looks like <project>. Use its project key?". Yes rewrites only `ProjectKey` in that clone's `configure.json` (commit it); No creates a separate project and is not asked again. Merging two projects that already exist is not supported.

## Concurrent Sessions

Several working copies may run DevCraft at the same time. Every change to `projects.json` is made while holding `features/projects.json.lock` and is written atomically (`projects.json.tmp`, then replaced). A session that cannot get the lock within about 2 seconds reports "projects.json is busy in another DevCraft session. Try again." and changes nothing. A leftover `.lock` file is harmless.

Agents that edit `projects.json` by hand do not take the lock: re-read the file right before editing and change only the value you mean to change (for example one feature's `status`).

## Start a Feature

1. Read the repository project key from `.devcraft/configure.json`.
2. Resolve that project key in the profile-level `features/projects.json`.
3. If the project is not listed, create a new project entry with a generated project key and write the same key to the repository configure file.
4. Create a feature entry under that project with a feature name, feature slug, short description, generated GUID folder name, and the optional work item ID and work item URL asked for by the New Feature form.
5. Create the feature artifact folder under profile-level `features/{feature-guid}`.
6. Store the standard DevCraft feature artifacts inside that GUID-named folder.

## List Features

`List features` (or `devcraft feature list`) starts with `Back` (return to the previous menu) and `Completed`, followed by the features that are in progress, meaning any status other than `Complete`. Choosing `Completed` shows only the `Complete` features, and `Back` there returns to the in-progress list.

The feature whose `branch` equals this working copy's current branch is listed first, marked `● … (current branch)`. In-progress features that other working copies have checked out show where, for example `→ Active Work 2`, with `(missing)` when that folder no longer exists. Working copies are named by the shortest unique name: the folder, or its parent folders when the folder repeats the repository name or collides with another copy.

## Feature Actions

Selecting a feature from the list opens a `Feature actions` menu:

- `Edit Feature Name` - asks for a new name and writes it to `feature-name` in profile `features/projects.json` (and `Name` in the repository `.devcraft/configure.json` for repo-central features). The slug, GUID folder name, storage type, work item, branch, status, and description do not change, and feature artifacts are not edited.
- `Edit Work Item` - a form pre-filled with the Work Item ID and Work Item URL. Emptied fields save `null`; an invalid URL reopens the form.
- `Set Feature to Current Branch` - links the working copy's current branch to the feature. A feature has at most one branch and a branch belongs to at most one feature: replacing the feature's branch, or taking the branch from another feature (which clears it there), asks first. Stops on a detached HEAD or outside git.
- `Clear Feature Branch` - shown when the feature has a branch; removes the link after confirmation. No git command runs.
- `Open Feature Directory using Code` - runs `code {feature-dir}` to open the feature's artifact folder in Visual Studio Code. System Central features open `features/{feature-guid}` in the profile DevCraft folder; Repo Central features open `.devcraft/features/{feature-guid}` in the repository. Features on external storage have no local directory.
- `Start a conversation about the feature` - opens the Starting Prompt editor (Ctrl+S to continue, Esc to cancel), selects the AI client, and starts a session for the feature with the operator's text appended as the final `Operator starting prompt:` section.
- `Commit Code` - commits all changes with an agent-written message and pushes to `origin`, creating the upstream when needed.
- `Switch and Pull` - switches to a local or `origin` branch and pulls it.
- `Create Branch` - creates and switches to a new branch named from the profile branch template or a custom name. `{WorkItem}` comes from the feature's work item ID. Creating a branch does not link it; use `Set Feature to Current Branch`.
- `Merge From` - commits the current changes, refreshes a source branch, and merges it into the current branch.
- `Back` - returns without starting anything.

Because a rename keeps the slug, agents can keep resolving a renamed feature by slug or by folder name.

## Working Copies

`Project Management` → `Remove Working Copy` lists the project's working copies (name, path, branch, last seen, and `missing` when the folder is gone) and removes the selected one after confirmation. The copy DevCraft is running in cannot be removed.

## Changing Feature Storage

`Configure DevCraft` → `Change Repository-Level Feature Storage` saves the new mode, then, when the new storage is Repo Central or System Central, offers a multi-select of active features stored in the other local type. Selected features are moved: every artifact is copied, checked by size and SHA-256, the index is updated, and only then is the old folder removed. A failure leaves the feature where it was. The run ends with a moved / not moved / failed report. Moving features to or from ADO, GitHub, or JIRA will come in a later feature; choosing one of those saves the mode and moves nothing.

## Edit Feature Artifacts

When DevCraft edits a feature artifact in System Central mode, it must:

1. Read the project key from the repository DevCraft configure file.
2. Resolve the matching project from profile-level `features/projects.json`.
3. Resolve the feature by slug within that project.
4. Open the artifact from profile-level `features/{feature-guid}`.
5. Save the updated artifact back to the same central feature folder.
6. Keep `features/projects.json` current.

## State Rules

- The repository DevCraft folder may still contain project-local configuration.
- Feature documentation is owned by the profile DevCraft folder.
- The repository configure file stores the project key used to find the project in the profile-level index.
- The project index must stay accurate enough for DevCraft to find the correct repository, project, feature, and GUID feature folder on the next run.
