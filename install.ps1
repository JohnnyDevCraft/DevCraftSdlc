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

    if (Test-Path $SourceProfile) {
        Get-ChildItem $SourceProfile -Directory -Recurse | ForEach-Object {
            $relativePath = [System.IO.Path]::GetRelativePath($SourceProfile, $_.FullName)
            New-Item -ItemType Directory -Force -Path (Join-Path $InstallDir $relativePath) | Out-Null
        }

        Get-ChildItem $SourceProfile -File -Recurse | Where-Object { $_.Name -ne "soul.md" } | ForEach-Object {
            $relativePath = [System.IO.Path]::GetRelativePath($SourceProfile, $_.FullName)
            $targetPath = Join-Path $InstallDir $relativePath
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $targetPath) | Out-Null
            Copy-Item $_.FullName $targetPath -Force
        }
    }

    @(
        "architectures",
        "feature-storage",
        "features",
        "project-types",
        "skills",
        "standards",
        "templates"
    ) | ForEach-Object {
        New-Item -ItemType Directory -Force -Path (Join-Path $InstallDir $_) | Out-Null
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
