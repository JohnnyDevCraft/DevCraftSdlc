# Feature Results: Recommend DevCraft For Unmanaged Folder

## Feature Reference

- Feature ID: 0003
- Feature Name: Recommend DevCraft For Unmanaged Folder
- State: Implementation

## Implementation Log

- 2026-10-07: Operator approved implementation of deterministic marker scanning for this feature.
- 2026-10-07: Added deterministic marker models and scanner for SDLC workflows and agent configuration markers.
- 2026-10-07: Added marker recommendation output after project scan results.
- 2026-10-07: Ensured marker scanning runs even when the folder readiness check reports ready, so hidden SDLC folders like `.specify` are still reported.
- 2026-10-07: Changed unmanaged-folder behavior from recommending DevCraft to automatically installing DevCraft.
- 2026-10-07: Added DevCraft project installer for `.devcraft/` control files and root `AGENT.md`, preserving existing files when present.
- 2026-10-07: Verified unmanaged empty folders install DevCraft, existing-code folders install DevCraft after a no-SDLC AI scan, and Spec Kit folders do not install DevCraft.
- 2026-10-07: Corrected AI-informed initialization so AI-provided project name and description are selectable suggestions; the operator can enter custom values before files are generated.
- 2026-10-07: Added repository `.devcraft/configure.json` creation during DevCraft installation.
- 2026-10-07: Rebuilt the macOS Apple Silicon binary at `artifacts/publish/osx-arm64/DevCraft.Cli`.

## Validation

- `dotnet test DevCraft.slnx`: Passed, 30 tests.
- `dotnet publish src/DevCraft.Cli/DevCraft.Cli.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o artifacts/publish/osx-arm64`: Passed.
- Controlled unmanaged empty folder: Passed, DevCraft installed.
- Controlled existing-code folder with no AI-driven SDLC: Passed, DevCraft installed.
- Controlled folder with `.specify/`: Passed, Spec Kit detected and DevCraft was not installed.
