# DevCraft

DevCraft is an AI-driven software delivery toolkit with a console-first workflow, profile-level skills, standards, architectures, project types, and feature storage modes.

## Install

### macOS and Linux

```sh
DEVCRAFT_REPOSITORY="JohnnyDevCraft/DevCraftSdlc" sh -c "$(curl -fsSL https://raw.githubusercontent.com/JohnnyDevCraft/DevCraftSdlc/master/install.sh)"
```

The installer creates `~/.DevCraft`, copies the DevCraft binary and profile seed files into it, preserves an existing `soul.md`, and adds `~/.DevCraft` to the shell path.

After installation, refresh your shell:

```sh
. ~/.zshrc
```

If you use Bash, refresh the Bash profile file that the installer reports.

### Windows PowerShell

```powershell
$env:DEVCRAFT_REPOSITORY="JohnnyDevCraft/DevCraftSdlc"; iwr https://raw.githubusercontent.com/JohnnyDevCraft/DevCraftSdlc/master/install.ps1 -UseB | iex
```

The installer creates the profile DevCraft folder under your user profile, copies `devcraft.exe` and the profile seed files into it, preserves an existing `soul.md`, and adds that folder to the user PATH.

Open a new PowerShell window after installation, then run:

```powershell
devcraft
```

## Release

DevCraft releases are published from Git tags.

1. Push the repository to GitHub.
2. Commit and push these changes.
3. Create and push a version tag:

```sh
git tag v0.1.0
git push origin v0.1.0
```

GitHub Actions will test the project, publish self-contained binaries for macOS, Linux, and Windows, package each binary with the `profile` seed folder and installer script, and attach the packages to the GitHub Release.
