# Feature Issues: Situational Awareness Logs

## ISSUE-001: Installer Latest Endpoint Skipped Beta 1

- Date: 2026-10-08
- Status: Resolved
- Context: After publishing `v1.0.0-beta.1` as a prerelease, the normal one-command installer installed `v1.0.0-alpha.9`.
- Cause: Both installers used GitHub's `/releases/latest` endpoint. GitHub excludes prereleases from that endpoint, so it returned the newest non-prerelease release instead of the beta.
- Resolution: Updated both installers to enumerate published releases, select the highest SemVer tag including prereleases, preserve explicit `DEVCRAFT_VERSION` overrides, and fetch the asset from the selected release tag.
- Validation: Added controlled shell and PowerShell release-selection tests; live shell resolution selected `v1.0.0-beta.1` and its `osx-arm64` asset before the Beta 2 release.
