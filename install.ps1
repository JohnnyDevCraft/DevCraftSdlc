$ErrorActionPreference = "Stop"

$Repository = if ($env:DEVCRAFT_REPOSITORY) { $env:DEVCRAFT_REPOSITORY } else { "JohnnyDevCraft/DevCraftSdlc" }
$InstallDir = if ($env:DEVCRAFT_HOME) { $env:DEVCRAFT_HOME } else { Join-Path $HOME ".DevCraft" }

function Fail($Message) {
    throw "DevCraft install failed: $Message"
}

function Get-Rid {
    $arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture

    switch ($arch) {
        "X64" { return "win-x64" }
        default { Fail "unsupported CPU architecture: $arch" }
    }
}

function Copy-Profile($SourceProfile) {
    New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null

    $soulPath = Join-Path $InstallDir "soul.md"
    $savedSoul = $null

    if (Test-Path $soulPath) {
        $savedSoul = Join-Path ([System.IO.Path]::GetTempPath()) "devcraft-soul-$([Guid]::NewGuid()).md"
        Copy-Item $soulPath $savedSoul -Force
    }

    if (Test-Path $SourceProfile) {
        Copy-Item (Join-Path $SourceProfile "*") $InstallDir -Recurse -Force
    }

    if ($savedSoul) {
        Copy-Item $savedSoul $soulPath -Force
    }
}

function Install-FromDirectory($PackageDir) {
    $binary = Join-Path $PackageDir "devcraft.exe"

    if (-not (Test-Path $binary)) {
        Fail "package binary not found"
    }

    Copy-Profile (Join-Path $PackageDir "profile")
    Copy-Item $binary (Join-Path $InstallDir "devcraft.exe") -Force
}

function Get-LatestAssetUrl($Rid) {
    $release = Invoke-RestMethod "https://api.github.com/repos/$Repository/releases/latest"
    $assetName = "DevCraft-$Rid.zip"
    $asset = $release.assets | Where-Object { $_.name -eq $assetName } | Select-Object -First 1

    if (-not $asset) {
        Fail "no release asset found for $Rid"
    }

    return $asset.browser_download_url
}

function Install-FromRelease {
    $rid = Get-Rid
    $temp = Join-Path ([System.IO.Path]::GetTempPath()) "devcraft-install-$([Guid]::NewGuid())"
    $archive = Join-Path $temp "DevCraft-$rid.zip"
    $packageDir = Join-Path $temp "package"

    New-Item -ItemType Directory -Force -Path $temp, $packageDir | Out-Null
    $assetUrl = Get-LatestAssetUrl $rid

    Write-Host "Downloading DevCraft for $rid..."
    Invoke-WebRequest $assetUrl -OutFile $archive
    Expand-Archive $archive -DestinationPath $packageDir -Force
    Install-FromDirectory $packageDir
}

function Add-ToPath {
    $currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
    $paths = $currentPath -split ";"

    if ($paths -notcontains $InstallDir) {
        $newPath = if ([string]::IsNullOrWhiteSpace($currentPath)) { $InstallDir } else { "$currentPath;$InstallDir" }
        [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
    }

    Write-Host "DevCraft installed in $InstallDir"
    Write-Host "Open a new PowerShell window, then run: devcraft"
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

if ((Test-Path (Join-Path $scriptDir "devcraft.exe")) -and (Test-Path (Join-Path $scriptDir "profile"))) {
    Install-FromDirectory $scriptDir
} else {
    Install-FromRelease
}

Add-ToPath
