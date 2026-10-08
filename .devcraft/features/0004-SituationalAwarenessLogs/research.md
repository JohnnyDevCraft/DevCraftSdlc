# Feature Research: Situational Awareness Logs

## Feature Reference

- Feature ID: 0004
- Feature Name: Situational Awareness Logs
- Spec: [spec.md](./spec.md)
- State: Implementation

## Research Summary

DevCraft situational awareness should use a storage-provider boundary so menu actions, compression, and AI handoffs work the same way regardless of whether the active store is profile-file JSON or MongoDB. The profile configuration should hold the operator-facing settings: `SituationEnabled`, `SituationScale`, `SituationStorage`, and `SituationConnection`.

The first implementation should default to file storage and keep MongoDB behind the same provider interface. This keeps local first-run behavior simple while leaving database storage available for operators who configure a connection string.

## Current Code Evidence

### Profile Configuration

- Profile configuration is represented by `DevCraftProfileConfiguration`.
- Profile configuration is read by `ProfileConfigurationReader` and written by `DevCraftProfileConfigurationWriter`.
- Profile setup and seeded profile files are created by `ProfileStructureInitializer`.
- Profile `configure.json` already carries reusable catalog data, supported clients, feature storage types, templates, skills, project types, standards, and architectures.

### Menus

- Main menu behavior is centralized in `DevCraftMenuCommand`.
- Configure DevCraft already has nested options for import, feature storage, and creation workflows.
- Skills and features already return to parent menus through the existing Back pattern.

### AI Handoffs

- Project and skill handoffs use `DevCraftAiSessionLauncher`.
- Feature handoffs use `FeatureAiSessionLauncher`.
- Situation context should be attached near the launcher layer so every AI handoff receives the same active situation context.

## Storage Design Findings

### File Storage

File storage should live under profile `.DevCraft/situation/` because the user described situational awareness as part of the profile-level DevCraft runtime, not a repository-local workflow artifact.

Recommended file layout:

- `people.json`
- `log-entries.json`
- `summaries.json`

The older draft split summaries into day/week/sprint/month/quarter/year files. The updated requirement defines three logical record groups: People, Log Entries, and Summaries. Using the same three groups for file storage and MongoDB keeps both providers aligned. If separate files per summary type are desired later, that can be added as an implementation detail behind the same provider.

### MongoDB Storage

MongoDB is a good match because situation data is document-shaped and already designed as JSON-like records.

Collections:

- `People`
- `LogEntries`
- `Summaries`

MongoDB implementation should be kept behind a provider interface. Unit tests should cover the provider contract without requiring a live database where practical. Live MongoDB validation can be manual or integration-test-only later.

## Configuration Findings

Recommended profile settings:

- `SituationEnabled`: Boolean.
- `SituationScale`: string enum, `sprint` or `weeks`.
- `SituationStorage`: string enum, `file` or `database`.
- `SituationConnection`: nullable string for MongoDB connection string.

Defaults:

- `SituationEnabled`: false unless the operator enables it.
- `SituationScale`: weeks unless the operator chooses sprints.
- `SituationStorage`: file.
- `SituationConnection`: empty/null.

## Compression Findings

Compression should operate on uncompressed records only.

For database storage:

- Read uncompressed source records.
- Send those source records to the AI client for summarization.
- Create a new summary record with the appropriate `Type`.
- Mark source records as compressed.

For file storage:

- Read uncompressed source records from file storage.
- Send those records to the AI client for summarization.
- Append a new summary record to file storage.
- Clear the source records that were summarized.

Scale behavior:

- `weeks`: log entries -> week summaries -> month summaries -> quarter summaries -> year summaries.
- `sprint`: log entries plus lower summaries -> sprint summaries -> quarter summaries -> year summaries.

## AI Handoff Findings

When `SituationEnabled` is true, every AI-agent handoff should include:

- all people records,
- all uncompressed log entries,
- all uncompressed summary records.

Compressed records should not be included because they represent archived context that has already been rolled up.

This should be added in the launcher layer to avoid duplicating context assembly in each menu command.

## Desktop Agent Instructions Findings

The profile should include a reusable text file containing copy/paste instructions for desktop agents. Configure DevCraft should expose an option named `Add DevCraft To Desktop Agent` that displays that text with a short instruction telling the operator to copy and paste it into their desktop agent instructions.

The source text should be generated into the profile folder during profile initialization so installers and upgrades can update it like other seeded profile files.

## Risks

- MongoDB support can pull in new dependencies and needs careful isolation so file storage remains simple and reliable.
- Connection strings are sensitive and should not be printed in normal console output.
- AI-generated summaries may be malformed or empty; source records must not be marked compressed or cleared unless a usable summary is created.
- Handoff prompts can become large if uncompressed context grows; later features may need size limits or summarization thresholds.

## Recommended Direction

1. Implement file storage first as the default path.
2. Add the profile settings and configuration menus in the same pass.
3. Add MongoDB through a provider boundary so database storage is not entangled with menus or compression logic.
4. Add situation context at the AI launcher layer.
5. Treat the beta 1 release bump as part of final validation.
