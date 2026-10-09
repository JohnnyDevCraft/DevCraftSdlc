# Feature Tasks: Situational Awareness Logs

## Feature Reference

- Feature ID: 0004
- Feature Name: Situational Awareness Logs
- Spec: [spec.md](./spec.md)
- Research: [research.md](./research.md)
- State: Implementation

## Planning Summary

- Planning goal: Add DevCraft situational-awareness configuration, people tracking, log entry tracking, scale-aware compression, AI handoff context, and desktop-agent setup instructions.
- Recommended implementation sequence: Add profile `Situation*` settings, add storage models, add file-storage structure, add configuration menus, add people and log entry actions, add scale-aware compression, add AI handoff context, add desktop-agent instruction display, then validate preservation and migration behavior.
- Known dependencies: Existing menu system, profile initialization, profile configuration, AI client selection and launcher infrastructure.
- TDD approach: For implementation changes, prove a focused red test or compile failure first, make the smallest implementation change to pass, then rerun focused and full validation before handoff.

## Phases

### Phase 1: Profile Configuration And Storage Models

- Purpose: Define how DevCraft configures and represents situational awareness.
- Expected Outcome: DevCraft has profile-level settings and one logical storage model for file and MongoDB implementations.
- Entry Criteria: Operator approves implementation of this feature.
- Exit Criteria: Configuration and model tests pass.

#### Application Development

- [ ] TASK-001 Add `SituationEnabled` to the profile configuration model.
- [ ] TASK-002 Add `SituationScale` to the profile configuration model with `sprint` and `weeks` values.
- [ ] TASK-003 Add `SituationStorage` to the profile configuration model with `file` and `database` values.
- [ ] TASK-004 Add `SituationConnection` to the profile configuration model.
- [ ] TASK-005 Write the `Situation*` settings to profile `configure.json`.
- [ ] TASK-006 Default `SituationEnabled` to off.
- [ ] TASK-007 Default `SituationStorage` to `file`.
- [ ] TASK-008 Default `SituationScale` to `weeks`.
- [ ] TASK-009 Define people records with `RowId`, `FirstName`, `LastName`, `Email`, `Phone`, `Relation`, and `Status`.
- [ ] TASK-010 Define log entry records with `RowId`, `DateTime`, `LogData`, and `IsCompressed`.
- [ ] TASK-011 Define summary records with `RowId`, `DateTime`, `SummaryData`, `Type`, and `IsCompressed`.

#### Tests And Validation

- [ ] TEST-001 Verify profile `configure.json` includes `SituationEnabled`.
- [ ] TEST-002 Verify profile `configure.json` includes `SituationScale`.
- [ ] TEST-003 Verify profile `configure.json` includes `SituationStorage`.
- [ ] TEST-004 Verify profile `configure.json` includes `SituationConnection`.
- [ ] TEST-005 Verify only supported scale and storage values are accepted.
- [ ] TEST-006 Verify situational awareness is disabled by default.
- [ ] TEST-007 Verify file storage is the default storage value.
- [ ] TEST-008 Verify weeks is the default scale value.
- [ ] TEST-009 Verify record models expose the required fields.

### Phase 2: File And MongoDB Storage Providers

- Purpose: Store situation data in profile files by default, and support MongoDB database storage.
- Expected Outcome: DevCraft can read and write people, log entries, and summaries through a storage abstraction.
- Entry Criteria: Phase 1 models exist.
- Exit Criteria: File storage is implemented and MongoDB storage behavior is designed/testable without requiring a live database for unit tests.

#### Application Development

- [ ] TASK-011 Implement profile `.DevCraft/situation/` as the file-storage location.
- [ ] TASK-012 Create file-storage people data.
- [ ] TASK-013 Create file-storage log entry data.
- [ ] TASK-014 Create file-storage summary data.
- [ ] TASK-015 Implement a situation storage abstraction used by menus, compression, and AI handoffs.
- [ ] TASK-016 Implement file-storage provider.
- [ ] TASK-017 Add MongoDB storage provider design for `People`, `LogEntries`, and `Summaries` collections.
- [ ] TASK-018 Validate that database storage has `SituationConnection` before writing data.
- [ ] TASK-019 When switching from file to database, migrate file people records into the People collection.
- [ ] TASK-020 When switching from file to database, migrate file log entry records into the LogEntries collection.
- [ ] TASK-021 When switching from file to database, migrate file week, sprint, month, quarter, and year records into the Summaries collection with matching `Type`.
- [ ] TASK-022 When switching from database to file, migrate MongoDB People records into file storage.
- [ ] TASK-023 When switching from database to file, migrate MongoDB LogEntries records into file storage.
- [ ] TASK-024 When switching from database to file, migrate MongoDB Summaries records into file storage.

#### Tests And Validation

- [ ] TEST-008 Verify file storage uses profile `.DevCraft/situation/`.
- [ ] TEST-009 Verify file storage creates missing situation data files.
- [ ] TEST-010 Verify existing file-storage data is not overwritten.
- [ ] TEST-011 Verify database storage without `SituationConnection` shows a clear configuration message.
- [ ] TEST-012 Verify file-to-database migration maps people records correctly.
- [ ] TEST-013 Verify file-to-database migration maps log entry records correctly.
- [ ] TEST-014 Verify file-to-database migration maps summary records with correct types.
- [ ] TEST-015 Verify database-to-file migration maps people records correctly.
- [ ] TEST-016 Verify database-to-file migration maps log entry records correctly.
- [ ] TEST-017 Verify database-to-file migration maps summary records correctly.

### Phase 3: Configure DevCraft Menus

- Purpose: Let the operator configure situational awareness and display desktop-agent setup instructions.
- Expected Outcome: Configure DevCraft includes Configure Situational Awareness and Add DevCraft To Desktop Agent.
- Entry Criteria: Configuration writer and storage providers exist.
- Exit Criteria: Menu tests cover all configuration paths.

#### Application Development

- [ ] TASK-025 Add Configure DevCraft > Configure Situational Awareness.
- [ ] TASK-026 Ask whether situational awareness is enabled.
- [ ] TASK-027 If enabled, ask whether to use sprints or weeks.
- [ ] TASK-028 If enabled, ask whether to use file or database storage.
- [ ] TASK-029 If database is selected, ask for and save `SituationConnection`.
- [ ] TASK-029A If database is selected, display concise copy/paste MongoDB Docker setup instructions before asking for the connection string.
- [ ] TASK-030 Run file-to-database migration when switching to database storage.
- [ ] TASK-031 Run database-to-file migration when switching to file storage.
- [ ] TASK-032 Create the profile desktop-agent instruction text file.
- [ ] TASK-033 Add Configure DevCraft > Add DevCraft To Desktop Agent.
- [ ] TASK-034 Display copy/paste instructions and the desktop-agent instruction text.

#### Tests And Validation

- [ ] TEST-018 Verify Configure DevCraft includes Configure Situational Awareness.
- [ ] TEST-019 Verify Configure Situational Awareness updates `SituationEnabled`.
- [ ] TEST-020 Verify Configure Situational Awareness updates `SituationScale`.
- [ ] TEST-021 Verify Configure Situational Awareness updates `SituationStorage`.
- [ ] TEST-022 Verify Configure Situational Awareness updates `SituationConnection` when database storage is selected.
- [ ] TEST-022A Verify database configuration displays MongoDB Docker setup instructions.
- [ ] TEST-023 Verify database selection triggers migration from file storage.
- [ ] TEST-024 Verify file selection triggers migration from database storage.
- [ ] TEST-025 Verify Configure DevCraft includes Add DevCraft To Desktop Agent.
- [ ] TEST-026 Verify Add DevCraft To Desktop Agent displays the profile instruction text.

### Phase 4: Logging Menu

- Purpose: Let the operator add people and log entries from DevCraft.
- Expected Outcome: The main menu includes Logging, and Logging can add people and log entries to the active storage provider.
- Entry Criteria: Storage abstraction exists.
- Exit Criteria: Logging menu tests pass.

#### Application Development

- [ ] TASK-031 Add `Logging` to the main menu.
- [ ] TASK-032 Add a Logging submenu with `Add Person`, `Add Log Entry`, scale-appropriate compression options, and `Back`.
- [ ] TASK-033 Show Compress Week and Compress Month only when `SituationScale` is `weeks`.
- [ ] TASK-034 Show Compress Sprint only when `SituationScale` is `sprint`.
- [ ] TASK-035 Always show Compress Quarter and Compress Year when situational awareness is enabled.
- [ ] TASK-036 Prompt for first name, last name, email, phone, relation, and status when adding a person.
- [ ] TASK-037 Save person records to the active situation storage provider.
- [ ] TASK-038 Prompt for log content when adding a log entry.
- [ ] TASK-039 Auto-set the log entry date when adding a log entry.
- [ ] TASK-040 Save non-empty log entries to the active situation storage provider.

#### Tests And Validation

- [ ] TEST-023 Verify the main menu includes Logging when situational awareness is enabled.
- [ ] TEST-024 Verify Logging includes Add Person.
- [ ] TEST-025 Verify Logging includes Add Log Entry.
- [ ] TEST-026 Verify compression menu visibility follows `SituationScale`.
- [ ] TEST-027 Verify a person record can be added.
- [ ] TEST-028 Verify a non-empty log entry can be added.
- [ ] TEST-029 Verify empty log content does not append a blank entry.
- [ ] TEST-030 Verify Back returns from Logging to the main menu.

### Phase 5: AI Compression Flow

- Purpose: Compress uncompressed lower-level records into higher-level summaries while preserving data on failure.
- Expected Outcome: Compression commands use AI handoff, save successful summaries, and either mark source records compressed or clear source files according to storage mode.
- Entry Criteria: Logging menu and storage provider work.
- Exit Criteria: Compression tests cover both scale paths and failure behavior.

#### Application Development

- [ ] TASK-041 Add a reusable compression service that reads uncompressed source records and writes target summary records.
- [ ] TASK-042 Add AI prompt construction for concise summary records.
- [ ] TASK-043 Include people records as structured prompt context.
- [ ] TASK-044 Add Compress Week behavior for weeks scale: read uncompressed log entries and create a week summary.
- [ ] TASK-045 Add Compress Sprint behavior for sprint scale: read uncompressed log entries and week summaries, and create a sprint summary.
- [ ] TASK-046 Add Compress Month behavior for weeks scale: read uncompressed week summaries and create a month summary.
- [ ] TASK-047 Add Compress Quarter behavior: read uncompressed month or sprint summaries based on `SituationScale` and create a quarter summary.
- [ ] TASK-048 Add Compress Year behavior: read uncompressed quarter summaries and create a year summary.
- [ ] TASK-049 For database storage, mark source records as compressed after successful compression.
- [ ] TASK-050 For file storage, clear source records after successful compression.
- [ ] TASK-051 Ensure empty source records show a clear message and do not launch AI.
- [ ] TASK-052 Ensure failed or empty AI summaries do not mark or clear source records.
- [ ] TASK-052A Parse compression AI summary JSON from both raw JSON and Markdown fenced JSON.

#### Tests And Validation

- [ ] TEST-031 Verify Compress Week creates a week summary from uncompressed log entries.
- [ ] TEST-032 Verify Compress Sprint creates a sprint summary from uncompressed lower records.
- [ ] TEST-033 Verify Compress Month creates a month summary from uncompressed week summaries.
- [ ] TEST-034 Verify Compress Quarter creates a quarter summary from uncompressed month or sprint summaries based on `SituationScale`.
- [ ] TEST-035 Verify Compress Year creates a year summary from uncompressed quarter summaries.
- [ ] TEST-036 Verify database compression marks source records as compressed.
- [ ] TEST-037 Verify file compression clears source records.
- [ ] TEST-038 Verify empty source records do not launch AI.
- [ ] TEST-039 Verify compression failure preserves source records.
- [ ] TEST-039A Verify compression summary parsing accepts Markdown fenced JSON.

### Phase 6: AI Handoff Context

- Purpose: Pass active situation data into every AI-agent handoff when situational awareness is enabled.
- Expected Outcome: AI handoff prompts include all people and uncompressed situation records.
- Entry Criteria: Situation storage providers can read people, log entries, and summaries.
- Exit Criteria: Handoff tests cover enabled and disabled situational awareness.

#### Application Development

- [ ] TASK-053 Load all people records for AI handoff context.
- [ ] TASK-054 Load all uncompressed log entry records for AI handoff context.
- [ ] TASK-055 Load all uncompressed summary records for AI handoff context.
- [ ] TASK-056 Add situation context to every AI-agent handoff when `SituationEnabled` is enabled.
- [ ] TASK-057 Omit situation context from AI-agent handoffs when `SituationEnabled` is disabled.

#### Tests And Validation

- [ ] TEST-040 Verify enabled situational awareness adds people to AI handoff prompts.
- [ ] TEST-041 Verify enabled situational awareness adds uncompressed log entries and summaries to AI handoff prompts.
- [ ] TEST-042 Verify compressed records are not passed to AI handoff prompts.
- [ ] TEST-043 Verify disabled situational awareness does not add situation context to AI handoff prompts.

### Final Phase: Validation And Release Prep

- Purpose: Prove the feature works and prepare it for release.

#### Application Validation

- [ ] VAL-001 Run the full automated test suite.
- [ ] VAL-002 Publish the local macOS binary for manual testing.
- [ ] VAL-003 Manually verify the Configure DevCraft situational awareness menus render cleanly.
- [ ] VAL-004 Manually verify the Logging menu renders cleanly.
- [ ] VAL-005 Manually verify a person and log entry write to the selected storage mode.
- [ ] VAL-006 Fix DevCraft ASCII header alignment so `Craft` lines up visually with `Dev`.
- [ ] VAL-007 Strip build metadata from the visible version line.
- [ ] VAL-008 Bump the DevCraft version to beta 1.
- [ ] VAL-009 Record created and modified files in `results.md`.
- [x] VAL-010 RED: Prove `devcraft -force` startup/install behavior is not supported by the existing CLI/startup flow.
- [x] VAL-011 GREEN: Implement `devcraft -force` so it installs `.devcraft` in the current folder without creating root `AGENT.md` or `AGENTS.md`.
- [x] VAL-012 REFACTOR: Update DevCraft shared/profile/current rules and templates so TDD evidence is required and root agent files are preserved.
- [x] VAL-013 Verify existing `.devcraft` takes precedence over other SDLC markers.
- [x] VAL-014 Bump DevCraft release metadata, docs, and tests to `1.0.0-beta.5`.
- [x] VAL-015 Mark prerelease GitHub releases automatically for prerelease tags.
- [x] VAL-016 RED: Prove the Unix installer overwrites an existing `devcraft` binary in place instead of replacing it with a new inode.
- [x] VAL-017 GREEN: Install Unix binaries through a staged temp file, macOS ad-hoc sign/verify, and atomic rename.
- [x] VAL-018 Validate a patched isolated macOS upgrade replaces the installed Mach-O inode, verifies code signing, and starts with the Beta 5 header.
- [x] VAL-019 RED: Prove situational handoff no longer may embed records and must describe file/database read paths.
- [x] VAL-020 GREEN: Change situational handoff to file-mode file guidance and database-mode Mongo read guidance with no connection string or record payload in the prompt.
- [x] VAL-021 Bump DevCraft release metadata, docs, and tests to `1.0.0-beta.6`.
- [x] VAL-022 RED: Prove the main menu lacks `Situational Conversation` and no planning handoff is launched.
- [x] VAL-023 GREEN: Add `Situational Conversation` using explicit client selection, disabled-state notice, and the common storage-aware AI handoff.
- [x] VAL-024 REFACTOR: Record validation evidence for the situational conversation menu flow without creating a release.
- [x] VAL-025 RED: Prove profile `projects.json` lacks conversational tracking status/schema support and the conversation handoff lacks tracking guidance.
- [x] VAL-026 GREEN: Add profile-only `projects.json` tracking shape with canonical DevCraft feature statuses and situational conversation guidance.
- [x] VAL-027 Bump DevCraft release metadata, docs, and tests to `1.0.0-beta.7`.
