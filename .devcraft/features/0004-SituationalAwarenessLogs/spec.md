# Feature Spec: Situational Awareness Logs

## Feature Reference

- Feature ID: 0004
- Feature Name: Situational Awareness Logs
- Type: Feature
- State: Implementation

Valid states: `Discovery`, `Clarification`, `Research`, `Planning`, `Analysis`, `Implementation`, `Complete`.
Code is only allowed while state is `Implementation`.

## Work Item

- Work Item Source: Operator conversation
- Work Item ID: N/A
- Work Item Title: Add situational awareness logging to DevCraft
- Work Item Summary: DevCraft should maintain rolling structured situation logs that capture day-to-day operator context and compress those logs into higher-level summaries over time.

## Overview

DevCraft needs situational awareness so AI agents can understand ongoing work, recurring people, and longer-running context without reading an unbounded raw log.

The feature adds profile-level situational awareness data for people, log entries, and summaries. Operators can add people and log entries through a new Logging menu. Operators can also compress uncompressed lower-level records into higher-level summaries using an AI client.

People records track `RowId`, `FirstName`, `LastName`, `Email`, `Phone`, `JobTitle`, `AssignedTeam`, `Organization`, `Relation`, `Status`, and `InactiveDate`. DevCraft can pass this people data into AI prompts as consumable structured data.

Log entry records track `RowId`, `DateTime`, `LogData`, and `IsCompressed`. Summary records track `RowId`, `DateTime`, `SummaryData`, `Type`, and `IsCompressed`, where `Type` is one of `week`, `sprint`, `month`, `quarter`, or `year`.

## Goals

- Create and maintain the DevCraft situation data structure.
- Add a Logging menu to the main DevCraft menu.
- Allow operators to add people and log entries.
- Use an AI client to compress logs into week, sprint, month, quarter, and year summaries.
- Mark summarized source records as compressed for database storage and clear summarized source files for file storage.
- Keep the logs available for future AI context.
- Support profile-level situational awareness enablement, scale, storage, and connection settings.
- Polish the shared DevCraft header before the beta 1 release.
- Ship the completed feature as DevCraft beta 1.

## Non-Goals

- Do not automatically schedule compression in this feature.
- Do not automatically infer calendar boundaries without operator action.
- Do not store logs outside the DevCraft situation folder in this feature.
- Do not implement background monitoring in this feature.
- Do not delete higher-level logs after compression.
- Do not implement database setup in the first file-storage pass unless database storage is approved for implementation.

## Current State

DevCraft has profile and repository configuration, startup menus, feature workflows, skills, project types, and AI-client handoffs. It does not have a situational-awareness log structure or a Logging menu.

## Target State

Profile `configure.json` includes:

- `SituationEnabled`
- `SituationScale`
- `SituationStorage`
- `SituationConnection`

`SituationEnabled` controls whether situational awareness is active and defaults to off. `SituationScale` is either `sprint` or `weeks`, where `sprint` shows sprint compression and `weeks` shows week and month compression. The default scale is `weeks`. `SituationStorage` is either `file` or `database`, and defaults to `file`. `SituationConnection` stores the MongoDB connection string used when `SituationStorage` is `database`.

When `SituationStorage` is `file`, DevCraft stores situation data under the profile DevCraft folder at `.DevCraft/situation/`. File storage uses structured JSON files. When one file is compressed into the next higher file, DevCraft clears the lower file and appends the new summary record to the higher file.

When `SituationStorage` is `database`, DevCraft uses MongoDB with three collections:

- `People` with `RowId`, `FirstName`, `LastName`, `Email`, `Phone`, `JobTitle`, `AssignedTeam`, `Organization`, `Relation`, `Status`, and `InactiveDate`.
- `LogEntries` with `RowId`, `DateTime`, `LogData`, and `IsCompressed`.
- `Summaries` with `RowId`, `DateTime`, `SummaryData`, `Type`, and `IsCompressed`.

For database compression, DevCraft reads uncompressed records from the next-lowest collection or summary type, passes them to AI for summarization, saves the summary to a new summary record, and marks the original records as compressed.

When situational awareness is enabled and the operator changes storage mode, DevCraft migrates existing situation data into the newly selected storage. Switching from file to database moves file-backed people, log entries, and summaries into MongoDB. Switching from database to file moves people plus only uncompressed MongoDB log entries and summaries back into profile file storage.

The main menu includes `Logging`. The Logging menu allows the operator to:

- Manage people.
- Add a log entry.
- Compress week.
- Compress sprint.
- Compress month.
- Compress quarter.
- Compress year.
- Go back.

The Manage People submenu includes `List People`, `Add Person`, and `Back`. `List People` starts with `Go Back`, then selectable people sorted by first name and last name in the format `FirstName LastName (Email) | JobTitle | Team | Org`. Selecting a person displays current values and supports editing name, contact, position, relationship details, and active/inactive status. Active and inactive states are stored separately from the inactive date so legacy status values can be normalized without losing existing records.

## Requirements

- Requirement 1: DevCraft must create the `situation` folder when setting up the applicable DevCraft runtime structure.
- Requirement 2: File storage must use profile `.DevCraft/situation/`.
- Requirement 3: The main DevCraft menu must include a `Logging` option.
- Requirement 4: The Logging menu must include options to manage people, add a log entry, compress week, compress sprint, compress month, compress quarter, compress year, and go back.
- Requirement 5: Manage People must include List People, Add Person, and Back.
- Requirement 6: Add Log Entry must auto-set the entry date and ask the operator for entry data.
- Requirement 7: Compress Week must be visible only when `SituationScale` is `weeks`.
- Requirement 8: Compress Sprint must be visible only when `SituationScale` is `sprint`.
- Requirement 9: Compress Month must be visible only when `SituationScale` is `weeks`.
- Requirement 10: Compress Quarter must compress uncompressed months or sprints based on `SituationScale`.
- Requirement 11: Compress Year must compress uncompressed quarters.
- Requirement 12: Sprint-based compression and month-based compression are alternative rollup paths; the operator decides which command to run.
- Requirement 13: Compression must not clear source logs if AI compression fails or produces no usable summary.
- Requirement 14: Each compression handoff must include enough instruction for the AI client to produce a concise structured summary record.
- Requirement 15: The AI client should receive relevant DevCraft context files when performing compression.
- Requirement 16: Empty source logs should produce a clear message instead of launching an AI client.
- Requirement 17: People data must support structured records with `RowId`, `FirstName`, `LastName`, `Email`, `Phone`, `JobTitle`, `AssignedTeam`, `Organization`, `Relation`, `Status`, and `InactiveDate`.
- Requirement 18: Log entry data must support structured records with `RowId`, `DateTime`, `LogData`, and `IsCompressed`.
- Requirement 19: Summary data must support structured records with `RowId`, `DateTime`, `SummaryData`, `Type`, and `IsCompressed`.
- Requirement 20: The profile-level DevCraft `configure.json` must include `SituationEnabled`, `SituationScale`, `SituationStorage`, and `SituationConnection`.
- Requirement 21: `SituationEnabled` must support yes/no semantics.
- Requirement 22: `SituationScale` must support `sprint` and `weeks`.
- Requirement 23: `SituationStorage` must support `file` and `database`.
- Requirement 24: `file` storage must use structured JSON files under the profile DevCraft folder at `.DevCraft/situation/`.
- Requirement 25: `database` storage must use MongoDB document storage.
- Requirement 26: If `SituationStorage` is `database` and no `SituationConnection` is configured, DevCraft must show a clear configuration message instead of attempting database writes.
- Requirement 27: The default `SituationStorage` value must be `file`.
- Requirement 28: Configure DevCraft must include Configure Situational Awareness.
- Requirement 29: Configure Situational Awareness must ask whether situational awareness is enabled.
- Requirement 30: If enabled, Configure Situational Awareness must ask whether to use sprints or months and weeks.
- Requirement 31: If enabled, Configure Situational Awareness must ask whether to use file or database storage.
- Requirement 32: If database storage is selected, Configure Situational Awareness must ask for the database connection string.
- Requirement 33: When situational awareness is enabled, every AI-agent handoff must include all people plus all uncompressed days, weeks, sprints, months, quarters, and years.
- Requirement 34: When passing situation to AI, DevCraft must send only uncompressed log and summary values.
- Requirement 35: The DevCraft ASCII header must render with the `Craft` portion visually aligned with `Dev`.
- Requirement 36: The visible version line must show only the semantic version and must not include build metadata after a plus sign.
- Requirement 37: When situational awareness is enabled and storage changes from file to database, DevCraft must migrate existing file-backed situation data into MongoDB.
- Requirement 38: When situational awareness is enabled and storage changes from database to file, DevCraft must migrate people plus only uncompressed MongoDB log entries and summaries into profile file storage.
- Requirement 39: The default `SituationEnabled` value must be off.
- Requirement 40: The default `SituationScale` value must be `weeks`.
- Requirement 41: When database storage is selected, DevCraft must display practical copy/paste MongoDB Docker setup instructions and the resulting connection string format without running Docker automatically.
- Requirement 42: AI responses parsed as JSON by situational awareness compression must support both raw JSON and Markdown fenced JSON.
- Requirement 43: The main DevCraft menu must include a `Situational Conversation` option that launches a user-selected installed AI client for non-project planning and memory-oriented conversation when situational awareness is enabled.
- Requirement 44: `Situational Conversation` must reuse the standard AI handoff context, including core soul, initialized instructions, DevCraft guidance, repository configuration, and the storage-aware situational-awareness access guidance.
- Requirement 45: `Situational Conversation` must not silently enable situational awareness or change situation storage/preferences. If situational awareness is disabled, DevCraft must return to the main menu with a clear configuration notice.
- Requirement 46: The profile feature tracking index at `.DevCraft/features/projects.json` must support conversational tracking fields: project `id`, `name`, `repo-location`, `repo-name`, and features with `id`, `feature-name`, `description`, `work-item-id`, and canonical DevCraft `status`.
- Requirement 47: The canonical feature status values for conversational tracking must match DevCraft feature states: `Discovery`, `Clarification`, `Research`, `Planning`, `Analysis`, `Implementation`, and `Complete`.
- Requirement 48: Add Person must ask for first name, last name, email, phone, job title, assigned team, organization, relationship details, and status. If status is inactive, it must ask for an inactive date.
- Requirement 49: List People must start with `Go Back`, then show people sorted by first name and last name in the format `FirstName LastName (Email) | JobTitle | Team | Org`.
- Requirement 50: Selecting a person must display current values and allow Edit Name, Edit Contact, Edit Position, Edit Relationship, Make Inactive or Make Active, and Go Back.
- Requirement 51: Editing a person must preserve `RowId`, update the existing record in the active storage provider, and avoid duplicate rows.
- Requirement 52: Make Inactive must set status to `INACTIVE` and store an inactive date. Make Active must set status to `ACTIVE` and clear the inactive date.
- Requirement 53: Legacy people records without position fields or inactive dates must still load, and recognized active/inactive status values should normalize to `ACTIVE` or `INACTIVE`.
- Requirement 54: AI handoff guidance must describe the new people fields, include all people as source data, and keep uncompressed-only filtering limited to log entries and summaries.

## Open Questions

- Should the `situation` folder live in the profile DevCraft folder, the repository `.devcraft` folder, or both?
- Should compressed summaries be generated by launching an interactive AI client or by using non-interactive scan-style client arguments?
- Should MongoDB storage also have a separate database name setting, or should the database name be part of the connection string?

## Clarification Log

### CL-001

- Question: What should situational awareness do?
- Answer: Maintain rolling logs for day, week, sprint, month, quarter, and year context, with AI-assisted compression between levels.
- Recommendation: N/A
- Decision: Add a new Logging menu and situation log structure.
- Spec Updates: Created this feature spec.

### CL-002

- Question: What information should `people.json` track?
- Answer: Track a name, email, description of the person, and description of the operator's relationship with that person.
- Recommendation: N/A
- Decision: Treat `people.json` as a structured people reference instead of only a freeform log.
- Spec Updates: Updated overview, requirements, and acceptance criteria.

### CL-003

- Question: Should people tracking remain Markdown?
- Answer: No. Change it to `people.json` so DevCraft can read and write it directly and pass it into prompts as structured data.
- Recommendation: N/A
- Decision: Use `people.json` instead of `people.md`.
- Spec Updates: Updated file list, requirements, open questions, tasks, and acceptance criteria.

### CL-004

- Question: Should the other log data be structured like `people.json`?
- Answer: Yes. Do the same kind of structured format for log data so DevCraft can consume it.
- Recommendation: N/A
- Decision: Use JSON files for day, week, sprint, month, quarter, and year logs.
- Spec Updates: Updated file list, requirements, open questions, user stories, and acceptance criteria.

### CL-005

- Question: How should DevCraft know where situational awareness data is stored?
- Answer: Add central profile configuration settings for situational awareness storage. Database storage should use MongoDB rather than EF Core with SQL Server or MySQL.
- Recommendation: Default to file storage for the first implementation because it keeps first-run setup simple and MongoDB can live behind the same provider boundary.
- Decision: Add profile storage settings and use MongoDB document storage for database mode.
- Spec Updates: Updated goals, target state, requirements, open questions, and acceptance criteria.

### CL-007

- Question: What additional setting is needed when database storage is selected?
- Answer: A connection string setting.
- Recommendation: Store it in profile `configure.json` with the other situational awareness settings, while avoiding printing it in normal console output.
- Decision: Add a MongoDB connection string requirement for database storage.
- Spec Updates: Updated target state, requirements, open questions, and acceptance criteria.

### CL-008

- Question: What should the default situational awareness storage be, and where should file storage live?
- Answer: Default to file storage. File storage writes the situation JSON files under the profile DevCraft folder in a folder named `situation`.
- Recommendation: N/A
- Decision: Default `SituationalAwarenessStorage` to `file` and store file-mode data in profile `.DevCraft/situation/`.
- Spec Updates: Updated target state, requirements, open questions, and acceptance criteria.

### CL-009

- Question: How should the operator change situational awareness storage?
- Answer: Add a Situational Awareness menu under Configure DevCraft with a storage option.
- Recommendation: N/A
- Decision: Configure DevCraft will expose a Situational Awareness submenu for storage configuration.
- Spec Updates: Updated requirements and acceptance criteria.

### CL-010

- Question: What settings belong in profile `configure.json`?
- Answer: Add `SituationEnabled`, `SituationScale`, `SituationStorage`, and `SituationConnection`.
- Recommendation: Use the operator-provided setting names directly in the profile configuration model.
- Decision: Replace the earlier `SituationalAwarenessStorage` naming with the four `Situation*` settings.
- Spec Updates: Updated target state, requirements, tasks, and acceptance criteria.

### CL-011

- Question: What should the Logging menu contain?
- Answer: Add Person, Add Log Entry, scale-appropriate compression commands, Compress Quarter, and Compress Year.
- Recommendation: Hide commands that do not apply to the configured scale.
- Decision: Logging menu visibility is driven by `SituationScale`.
- Spec Updates: Updated target state, requirements, user stories, and acceptance criteria.

### CL-012

- Question: What records should file and database storage represent?
- Answer: People, Log Entries, and Summaries. Database storage has three collections: People, Log Entries, and Summaries.
- Recommendation: Use the same logical record model for file and database storage so the rest of DevCraft can treat storage uniformly.
- Decision: Situation storage is based on people records, log entry records, and summary records.
- Spec Updates: Updated overview, target state, requirements, and acceptance criteria.

### CL-013

- Question: What situation data should be handed to AI agents?
- Answer: When enabled, pass all people and all uncompressed days, weeks, sprints, months, quarters, and years.
- Recommendation: Keep compressed records out of prompts so the agent receives only active context.
- Decision: AI handoffs include people and uncompressed situation records only.
- Spec Updates: Updated requirements and acceptance criteria.

### CL-014

- Question: Should the DevCraft header alignment and version suffix fix be part of this feature?
- Answer: Yes. It should go out with beta 1.
- Recommendation: Treat this as beta 1 release polish inside feature 0004 rather than a separate feature.
- Decision: Add header alignment and semantic-only version display to feature 0004 scope.
- Spec Updates: Updated goals, requirements, tasks, and acceptance criteria.

### CL-015

- Question: What should happen to existing data when situational awareness storage changes?
- Answer: Migrate existing data into the newly selected storage in both directions, but database-to-file only brings uncompressed records back.
- Recommendation: Run migration as part of the Configure Situational Awareness storage switch before saving the final storage setting.
- Decision: Storage migration must support file-to-database and database-to-file, with database-to-file limited to uncompressed records.
- Spec Updates: Updated target state, requirements, tasks, and acceptance criteria.

### CL-016

- Question: What are the default situational awareness settings?
- Answer: Situational awareness is off, file-based, and uses weeks and months.
- Recommendation: Store those defaults directly in profile `configure.json` during profile initialization.
- Decision: Defaults are `SituationEnabled` off, `SituationStorage` file, and `SituationScale` weeks.
- Spec Updates: Updated target state, requirements, tasks, and acceptance criteria.

### CL-017

- Question: What help should DevCraft provide when database storage is selected?
- Answer: Display concise copy/paste MongoDB Docker setup instructions and a connection string.
- Recommendation: Do not run Docker automatically; show commands only.
- Decision: The database configuration path displays Docker setup guidance before asking for the connection string.
- Spec Updates: Added Requirement 41.

### CL-018

- Question: How should situational awareness parse AI JSON?
- Answer: Support both raw JSON and Markdown fenced JSON.
- Recommendation: Reuse the same normalization pattern used for project scan JSON.
- Decision: Compression summary parsing extracts JSON from raw or fenced responses before deserialization.
- Spec Updates: Added Requirement 42.

## Standards Application

- Relevant shared standards from `codex-setup`: `/Users/john/codex-setup/standards/CSharp.md`
- How those standards apply to this feature: Future implementation must keep C# types organized one per file and keep console behavior testable through focused tests.
- Any project-specific standards extensions: DevCraft state gates remain binding.
- Any conflicts or special handling to account for: This feature must not enter implementation until the operator approves the task list.

## New And Modified Views / Pages

- `Main Menu` - `Modified`
- `Logging Menu` - `New`
- `Configure DevCraft > Situational Awareness Menu` - `New`
- `Shared DevCraft Header` - `Modified`

## Module User Stories

### Situational Logging

#### Story 0: Start Situational Conversation

- As an operator
- I want to start a situational conversation from the main menu
- So that an AI agent can help me plan the day and remember available context without starting project discovery or feature implementation

##### Happy Path Tests

- Test 1: The main menu includes `Situational Conversation`.
- Test 2: `Situational Conversation` launches the selected installed AI client with planning and context-recall instructions.

##### Edge Case Tests

- Test 1: Disabled situational awareness shows a configuration notice and does not launch an AI client.

#### Story 1: Add Daily Log Entry

- As an operator
- I want to add a situational log entry from DevCraft
- So that important daily context is captured for future AI awareness

##### Happy Path Tests

- Test 1: The Logging menu appends a dated log entry record.

##### Edge Case Tests

- Test 1: Empty log input does not append a blank entry.

#### Story 2: Add Person

- As an operator
- I want to add people to situational awareness
- So that AI agents have context about the people involved in my work

##### Happy Path Tests

- Test 1: The Logging menu adds a person record with first name, last name, email, phone, relation, and status.

##### Edge Case Tests

- Test 1: Missing required person fields show a clear message.

### Log Compression

#### Story 3: Compress Logs Into Summaries

- As an operator
- I want to compress lower-level logs into higher-level summaries
- So that DevCraft preserves long-term context without keeping every raw detail forever

##### Happy Path Tests

- Test 1: Compress Week summarizes uncompressed log entries, creates a week summary, and marks or clears the source records depending on storage mode.
- Test 2: Compress Sprint summarizes uncompressed log entries and week summaries, creates a sprint summary, and marks or clears the source records depending on storage mode.
- Test 3: Compress Month summarizes uncompressed week summaries, creates a month summary, and marks or clears the source records depending on storage mode.
- Test 4: Compress Quarter summarizes uncompressed month or sprint summaries based on `SituationScale`, creates a quarter summary, and marks or clears the source records depending on storage mode.
- Test 5: Compress Year summarizes uncompressed quarter summaries, creates a year summary, and marks or clears the source records depending on storage mode.

##### Negative Tests

- Test 1: Compression failure leaves source logs unchanged.
- Test 2: Empty source logs do not launch an AI client.

## Acceptance Criteria

- [ ] DevCraft creates the situation log structure.
- [ ] Main menu includes `Logging`.
- [ ] Logging menu can append a daily log entry.
- [ ] Compression commands launch the selected AI client with the correct source logs and summary target.
- [ ] Successful compression appends a summary to the target log.
- [ ] Successful compression clears the summarized source logs.
- [ ] Failed compression preserves source logs.
- [ ] Empty source logs do not launch an AI client.
- [ ] `people.json` provides structured fields for name, email, person description, and relationship description.
- [ ] `people.json` is valid JSON and can be included in AI prompts as structured data.
- [ ] Situation log data files are valid JSON and can be included in AI prompts as structured data.
- [ ] Profile `configure.json` includes `SituationEnabled`, `SituationScale`, `SituationStorage`, and `SituationConnection`.
- [ ] `SituationScale` supports `sprint` and `weeks`.
- [ ] `SituationStorage` supports `file` and `database`.
- [ ] Default `SituationEnabled` value is off.
- [ ] Default `SituationScale` value is `weeks`.
- [ ] Default `SituationStorage` value is `file`.
- [ ] File storage writes situation data under profile `.DevCraft/situation/`.
- [ ] Database storage design uses MongoDB document storage.
- [ ] Profile `configure.json` includes a MongoDB connection string setting for database storage as `SituationConnection`.
- [ ] Missing connection string blocks database writes with a clear message.
- [ ] Configure DevCraft includes a Situational Awareness menu with a storage option.
- [ ] Logging menu can add people.
- [ ] Logging menu compression options are visible based on `SituationScale`.
- [ ] Main menu includes `Situational Conversation`.
- [ ] Situational Conversation launches the selected installed AI client with day-planning and context-recall instructions.
- [ ] Situational Conversation gives a clear configuration notice instead of launching an AI client when situational awareness is disabled.
- [ ] Profile `projects.json` tracks project and feature conversation context with canonical DevCraft feature statuses.
- [ ] AI-agent handoffs include all people and uncompressed situation records when situation awareness is enabled.
- [ ] The completed release is versioned as beta 1.
- [ ] DevCraft ASCII header alignment is corrected.
- [ ] Visible version line omits build metadata.
- [ ] File-to-database storage switch migrates existing situation data.
- [ ] Database-to-file storage switch migrates people plus only uncompressed situation records.
