# Feature Results: Startup Soul And Folder Scan

## Feature Reference

- Feature ID: 0002
- Feature Name: Startup Soul And Folder Scan
- State: Implementation

## Implementation Log

- 2026-10-07: Operator approved the task list and authorized implementation.
- 2026-10-07: Added startup context resolution for current folder, profile `.DevCraft`, and `soul.md`.
- 2026-10-07: Added profile and soul file detection.
- 2026-10-07: Added soul setup content generation and first-run prompt plumbing.
- 2026-10-07: Added terminal AI agent detection for Codex, Claude AI, and GitHub Copilot.
- 2026-10-07: Added current-folder readiness detection that ignores hidden files.
- 2026-10-07: Added AI project scan prompt generation, terminal-agent scanner support, JSON parsing, and console table display.
- 2026-10-07: Added focused unit tests for profile detection, soul writing, agent detection, folder readiness, AI prompt content, JSON parsing, and startup flow behavior.
- 2026-10-07: Ran `dotnet test DevCraft.slnx`; all 15 tests passed.
- 2026-10-07: Ran the CLI against an empty temporary folder; it reported `Folder ready for DevCraft`.
- 2026-10-07: Ran the CLI against a temporary folder with a non-hidden file and controlled AI JSON; it reported `Folder has code`, displayed project `Sample` as `.NET console app`, and displayed `AI-driven SDLC detected: SpecKit`.
- 2026-10-07: Corrected first-run behavior so a missing profile `.DevCraft` folder is created before soul setup runs.
- 2026-10-07: Added profile support folder initialization for `skills`, `standards`, and `architectures` under the profile `.DevCraft` folder.
- 2026-10-07: Added profile `configure.json` generation with skills, standards, and architecture document names, descriptions, and profile-relative paths.
- 2026-10-07: Added lowercase kebab-case slugs to `configure.json` entries for future import/export references.
- 2026-10-07: Added `list` command category parsing and console output for skills, standards, and architectures.
- 2026-10-07: Excluded `README.md` and underscore-prefixed Markdown template files from profile `configure.json` catalog entries.
- 2026-10-07: Added profile `templates/skill-template.md` copied from the shared skill template for future skill creation.
- 2026-10-07: Added the profile `project-types` folder to match the planned documentation repository shape.
- 2026-10-07: Added templates, project types, and supported terminal clients to profile `configure.json`.
- 2026-10-07: Added profile `initialized.md` with guidance for passing `soul.md` and `configure.json` to terminal AI clients.
- 2026-10-07: Added profile root `DevCraft.md` copied from the shared DevCraft mode rules.
- 2026-10-07: Added profile `feature-storage` folder for storage-mode guidance such as ADO work item attachment storage.
- 2026-10-07: Added feature storage types and selected feature storage to profile `configure.json`.
- 2026-10-07: Split supported terminal client configuration into scan and session operation objects.
- 2026-10-07: Removed forced read-only sandbox mode from Codex scan configuration and invocation.
- 2026-10-07: Corrected folder readiness detection to scan visible subfolders recursively, while still ignoring hidden folders and common generated folders.
- 2026-10-07: Updated folder readiness detection so any non-hidden folder entry counts as existing content, even if the folder contains no files.
- 2026-10-07: Removed the unsupported `--ask-for-approval never` argument from the Codex scanner launch.
- 2026-10-07: Added graceful AI scan failure handling so terminal agent failures are reported in the console instead of aborting DevCraft.
- 2026-10-07: Rebuilt the macOS Apple Silicon binary at `artifacts/publish/osx-arm64/DevCraft.Cli`.
- 2026-10-07: Switched Codex scanning to JSON event mode and added a Spectre status spinner while the folder scan runs.

## Created Files

- `src/DevCraft.Cli/StartupContext.cs` - Startup path model.
- `src/DevCraft.Cli/StartupContextResolver.cs` - Resolves current folder, profile `.DevCraft`, and `soul.md` path.
- `src/DevCraft.Cli/ProfileState.cs` - Profile state model.
- `src/DevCraft.Cli/ProfileStateDetector.cs` - Detects profile folder and soul file presence.
- `src/DevCraft.Cli/ProfileStructureInitializer.cs` - Ensures profile support folders exist.
- `src/DevCraft.Cli/ProfileDocumentCatalogBuilder.cs` - Builds profile document catalog entries from Markdown files.
- `src/DevCraft.Cli/DevCraftProfileConfiguration.cs` - Profile configuration model.
- `src/DevCraft.Cli/SlugGenerator.cs` - Generates lowercase kebab-case catalog slugs.
- `src/DevCraft.Cli/ListCommand.cs` - Lists configured skills, standards, and architectures.
- `src/DevCraft.Cli/TerminalAgentKind.cs` - Supported terminal AI agent enum.
- `src/DevCraft.Cli/TerminalAgent.cs` - Terminal AI agent model.
- `src/DevCraft.Cli/TerminalAgentCatalog.cs` - Codex, Claude AI, and GitHub Copilot detection catalog.
- `src/DevCraft.Cli/CommandLocator.cs` - PATH-based command lookup.
- `src/DevCraft.Cli/FolderReadiness.cs` - Folder readiness enum.
- `src/DevCraft.Cli/FolderReadinessResult.cs` - Folder readiness result model.
- `src/DevCraft.Cli/FolderReadinessDetector.cs` - Read-only folder scan logic.
- `src/DevCraft.Cli/SoulSetupAnswers.cs` - Soul setup answer model.
- `src/DevCraft.Cli/SoulFileWriter.cs` - `soul.md` content writer.
- `src/DevCraft.Cli/DetectedProject.cs` - Detected project model.
- `src/DevCraft.Cli/AiSdlcDetection.cs` - AI-driven SDLC detection model.
- `src/DevCraft.Cli/ProjectScanResult.cs` - Project scan result model.
- `src/DevCraft.Cli/ProjectScanJsonParser.cs` - AI JSON parser.
- `src/DevCraft.Cli/ProjectScanJson.cs` - AI JSON DTO.
- `src/DevCraft.Cli/ProjectJson.cs` - Detected project JSON DTO.
- `src/DevCraft.Cli/AiDrivenSdlcJson.cs` - AI-driven SDLC JSON DTO.
- `src/DevCraft.Cli/ProjectScanPromptBuilder.cs` - Existing-folder AI scan prompt builder.
- `src/DevCraft.Cli/IAiProjectScanner.cs` - Project scanner abstraction.
- `src/DevCraft.Cli/TerminalAiProjectScanner.cs` - Terminal-agent scanner using controlled JSON or supported CLI clients.
- `src/DevCraft.Cli/IConsoleInteraction.cs` - Console interaction abstraction.
- `src/DevCraft.Cli/SpectreConsoleInteraction.cs` - Spectre.Console interaction implementation.
- `src/DevCraft.Cli/StartupFlow.cs` - Orchestrates profile, soul, folder, and AI scan startup flow.
- `src/DevCraft.Cli/ConsoleProjectScanWriter.cs` - Displays detected projects and AI-driven SDLC status.
- `tests/DevCraft.Cli.Tests/TestDirectory.cs` - Temporary directory test helper.
- `tests/DevCraft.Cli.Tests/ProfileStateDetectorTests.cs` - Profile detection tests.
- `tests/DevCraft.Cli.Tests/FolderReadinessDetectorTests.cs` - Folder readiness tests.
- `tests/DevCraft.Cli.Tests/SoulFileWriterTests.cs` - Soul content tests.
- `tests/DevCraft.Cli.Tests/TerminalAgentCatalogTests.cs` - Terminal agent detection catalog tests.
- `tests/DevCraft.Cli.Tests/ProjectScanJsonParserTests.cs` - AI JSON parser tests.
- `tests/DevCraft.Cli.Tests/ProjectScanPromptBuilderTests.cs` - AI prompt tests.
- `tests/DevCraft.Cli.Tests/FakeConsoleInteraction.cs` - Console test double.
- `tests/DevCraft.Cli.Tests/FakeAiProjectScanner.cs` - AI scanner test double.
- `tests/DevCraft.Cli.Tests/ThrowingAiProjectScanner.cs` - AI scanner failure test double.
- `tests/DevCraft.Cli.Tests/StartupFlowTests.cs` - Startup flow tests.

## Modified Files

- `src/DevCraft.Cli/DevCraftCli.cs` - Runs the startup flow after the logo.
- `src/DevCraft.Cli/TerminalAiProjectScanner.cs` - Removed unsupported Codex argument.
- `src/DevCraft.Cli/StartupFlow.cs` - Added graceful scan failure handling.
- `tests/DevCraft.Cli.Tests/StartupFlowTests.cs` - Added graceful scan failure coverage.
- `.devcraft/AGENT.md` - Tracked active feature implementation status.
- `.devcraft/features/0002-DetectCurrentFolderProjectState/spec.md` - Moved feature into Implementation.
- `.devcraft/features/0002-DetectCurrentFolderProjectState/tasks.md` - Marked implementation and validation tasks complete.

## Validation Evidence

- Build and automated tests: `dotnet test DevCraft.slnx` passed with 20 tests.
- Empty-folder run: CLI displayed `Folder ready for DevCraft`.
- Existing-folder run with controlled AI JSON: CLI displayed `Folder has code`, a project table, and `AI-driven SDLC detected: SpecKit`.
