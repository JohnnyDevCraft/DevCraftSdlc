$ErrorActionPreference = "Stop"

$env:DEVCRAFT_INSTALLER_TEST_MODE = "1"
. (Join-Path $PSScriptRoot ".." "install.ps1")

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
    [PSCustomObject]@{ tag_name = "v0.9.0"; draft = $false }
)

Assert-Equal "v1.0.0-beta.8" (Select-ReleaseTag $releases) "selects newest beta over alpha releases"
Assert-True (Test-VersionGreater "v1.0.0-beta.1" "v1.0.0-alpha.12") "beta sorts after alpha 12"
Assert-True (Test-VersionGreater "v1.0.0-beta.2" "v1.0.0-beta.1") "beta 2 sorts after beta 1"
Assert-True (Test-VersionGreater "v1.0.0-beta.5" "v1.0.0-beta.2") "beta 5 sorts after beta 2"
Assert-True (Test-VersionGreater "v1.0.0-beta.7" "v1.0.0-beta.6") "beta 7 sorts after beta 6"
Assert-True (Test-VersionGreater "v1.0.0-beta.8" "v1.0.0-beta.7") "beta 8 sorts after beta 7"
Assert-True (Test-VersionGreater "v1.0.0-alpha.12" "v1.0.0-alpha.9") "alpha 12 sorts after alpha 9"

$env:DEVCRAFT_VERSION = "1.0.0-alpha.12"
Assert-Equal "v1.0.0-alpha.12" (Select-ReleaseTag $releases) "honors explicit version override"
Remove-Item Env:\DEVCRAFT_VERSION

Write-Host "InstallerReleaseSelection.Tests.ps1 passed"
