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

The `projects.json` file is the system-level index of projects known to DevCraft. Each project entry has a project key, project name, project slug, repository path, and an array of features.

Each feature entry has:

- Feature name
- Feature slug
- Feature short description
- Folder name {feature-guid}

The project key and each feature folder name are randomly generated GUID values. The same project key is also stored in the repository DevCraft folder's `.devcraft/configure.json`, which lets DevCraft map the current repository back to the matching project entry in the profile-level `features/projects.json`.

## Start a Feature

1. Read the repository project key from `.devcraft/configure.json`.
2. Resolve that project key in the profile-level `features/projects.json`.
3. If the project is not listed, create a new project entry with a generated project key and write the same key to the repository configure file.
4. Create a feature entry under that project with a feature name, feature slug, short description, and generated GUID folder name.
5. Create the feature artifact folder under profile-level `features/{feature-guid}`.
6. Store the standard DevCraft feature artifacts inside that GUID-named folder.

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
