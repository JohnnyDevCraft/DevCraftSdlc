$ErrorActionPreference = "Stop"

$env:DEVCRAFT_INSTALLER_TEST_MODE = "1"
. (Join-Path (Split-Path -Parent $PSScriptRoot) "install.ps1")

function Assert-Equal($Expected, $Actual, $Description) {
    if ($Expected -ne $Actual) {
        throw "$Description expected '$Expected' but got '$Actual'"
    }
}

function Assert-True($Value, $Description) {
    if (-not $Value) {
        throw $Description
    }
}

$releases = @(
    [PSCustomObject]@{ tag_name = "v1.0.0-alpha.12"; draft = $false },
    [PSCustomObject]@{ tag_name = "v1.0.0-alpha.9"; draft = $false },
    [PSCustomObject]@{ tag_name = "v1.0.0-beta.1"; draft = $false },
    [PSCustomObject]@{ tag_name = "v1.0.0-beta.2"; draft = $false },
    [PSCustomObject]@{ tag_name = "v1.0.0-beta.5"; draft = $false },
    [PSCustomObject]@{ tag_name = "v1.0.0-beta.6"; draft = $false },
    [PSCustomObject]@{ tag_name = "v1.0.0-beta.7"; draft = $false },
    [PSCustomObject]@{ tag_name = "v1.0.0-beta.8"; draft = $false },
    [PSCustomObject]@{ tag_name = "v1.0.0-beta.16"; draft = $false },
    [PSCustomObject]@{ tag_name = "v0.9.0"; draft = $false }
)

Assert-Equal "v1.0.0-beta.16" (Select-ReleaseTag $releases) "selects newest beta over alpha releases"
Assert-True (Test-VersionGreater "v1.0.0-beta.1" "v1.0.0-alpha.12") "beta sorts after alpha 12"
Assert-True (Test-VersionGreater "v1.0.0-beta.2" "v1.0.0-beta.1") "beta 2 sorts after beta 1"
Assert-True (Test-VersionGreater "v1.0.0-beta.5" "v1.0.0-beta.2") "beta 5 sorts after beta 2"
Assert-True (Test-VersionGreater "v1.0.0-beta.7" "v1.0.0-beta.6") "beta 7 sorts after beta 6"
Assert-True (Test-VersionGreater "v1.0.0-beta.8" "v1.0.0-beta.7") "beta 8 sorts after beta 7"
Assert-True (Test-VersionGreater "v1.0.0-beta.16" "v1.0.0-beta.15") "beta 16 sorts after beta 15"
Assert-True (Test-VersionGreater "v1.0.0-alpha.12" "v1.0.0-alpha.9") "alpha 12 sorts after alpha 9"

$env:DEVCRAFT_VERSION = "1.0.0-alpha.12"
Assert-Equal "v1.0.0-alpha.12" (Select-ReleaseTag $releases) "honors explicit version override"
Remove-Item Env:\DEVCRAFT_VERSION

Assert-Equal "win-x64" (Resolve-Rid @("AMD64")) "maps the Windows AMD64 processor name to win-x64"
Assert-Equal "win-x64" (Resolve-Rid @("X64")) "maps the .NET X64 architecture name to win-x64"
Assert-Equal "win-x64" (Resolve-Rid @($null, "", " amd64 ")) "skips empty readings and ignores case and spacing"
Assert-Equal "win-x64" (Resolve-Rid @("x86", "AMD64")) "a 32-bit shell on 64-bit Windows still resolves win-x64"
Assert-True ($null -eq (Resolve-Rid @("ARM64"))) "ARM64 is not supported"
Assert-True ($null -eq (Resolve-Rid @($null, ""))) "no reading resolves nothing"

$temp = Join-Path ([System.IO.Path]::GetTempPath()) "devcraft-installer-test-$([Guid]::NewGuid())"
$InstallDir = Join-Path $temp "install-root"
$sourceProfile = Join-Path (Join-Path $temp "package") "profile"
New-Item -ItemType Directory -Force -Path $InstallDir, $sourceProfile | Out-Null
@'
{
  "SituationEnabled": true,
  "SituationScale": "sprint",
  "SituationStorage": "database",
  "SituationConnection": "mongodb://localhost:27017/ExistingDevCraft"
}
'@ | Set-Content (Join-Path $InstallDir "configure.json")
@'
{
  "SituationEnabled": false,
  "SituationScale": "weeks",
  "SituationStorage": "file",
  "SituationConnection": null
}
'@ | Set-Content (Join-Path $sourceProfile "configure.json")

Copy-Profile $sourceProfile

$preserved = Get-Content (Join-Path $InstallDir "configure.json") -Raw | ConvertFrom-Json
Assert-True $preserved.SituationEnabled "install preserves existing situational awareness enablement"
Assert-Equal "sprint" $preserved.SituationScale "install preserves existing situational awareness scale"
Assert-Equal "database" $preserved.SituationStorage "install preserves existing situational awareness storage"
Assert-Equal "mongodb://localhost:27017/ExistingDevCraft" $preserved.SituationConnection "install preserves existing situational awareness connection"

Write-Host "InstallerReleaseSelection.Tests.ps1 passed"
