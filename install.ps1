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

function Get-NormalizedVersionTag($Version) {
    if ($Version.StartsWith("v")) {
        return $Version
    }

    return "v$Version"
}

function Get-VersionParts($Tag) {
    $normalized = Get-NormalizedVersionTag $Tag
    $value = $normalized.Substring(1)
    $pieces = $value -split "-", 2
    $core = $pieces[0] -split "\."
    $preLabel = ""
    $preNumber = 0
    $preRank = 9

    if ($pieces.Count -gt 1) {
        $prePieces = $pieces[1] -split "\.", 2
        $preLabel = $prePieces[0]

        if ($prePieces.Count -gt 1 -and [int]::TryParse($prePieces[1], [ref]$preNumber)) {
            $preNumber = [int]$prePieces[1]
        }

        $preRank = switch ($preLabel) {
            "alpha" { 1 }
            "beta" { 2 }
            "rc" { 3 }
            default { 0 }
        }
    }

    [PSCustomObject]@{
        Major = [int]$core[0]
        Minor = [int]$core[1]
        Patch = [int]$core[2]
        PreRank = $preRank
        PreNumber = $preNumber
    }
}

function Test-VersionGreater($Left, $Right) {
    $leftParts = Get-VersionParts $Left
    $rightParts = Get-VersionParts $Right

    foreach ($part in @("Major", "Minor", "Patch", "PreRank", "PreNumber")) {
        if ($leftParts.$part -gt $rightParts.$part) {
            return $true
        }

        if ($leftParts.$part -lt $rightParts.$part) {
            return $false
        }
    }

    return $false
}

function Select-ReleaseTag($Releases) {
    if ($env:DEVCRAFT_VERSION) {
        return Get-NormalizedVersionTag $env:DEVCRAFT_VERSION
    }

    $selected = $null

    foreach ($release in $Releases) {
        if ($release.draft) {
            continue
        }

        if ($release.tag_name -notmatch '^v\d+\.\d+\.\d+(-[A-Za-z]+\.\d+)?$') {
            continue
        }

        if (-not $selected -or (Test-VersionGreater $release.tag_name $selected)) {
            $selected = $release.tag_name
        }
    }

    if (-not $selected) {
        Fail "no published DevCraft release found"
    }

    return $selected
}

function Get-ReleaseTag {
    if ($env:DEVCRAFT_VERSION) {
        return Get-NormalizedVersionTag $env:DEVCRAFT_VERSION
    }

    $releases = Invoke-RestMethod "https://api.github.com/repos/$Repository/releases?per_page=100"
    return Select-ReleaseTag $releases
}

function Get-AssetUrl($Rid, $ReleaseTag) {
    $release = Invoke-RestMethod "https://api.github.com/repos/$Repository/releases/tags/$ReleaseTag"
    $assetName = "DevCraft-$Rid.zip"
    $asset = $release.assets | Where-Object { $_.name -eq $assetName } | Select-Object -First 1

    if (-not $asset) {
        Fail "no release asset found for $Rid in $ReleaseTag"
    }

    return $asset.browser_download_url
}

function Install-FromRelease {
    $rid = Get-Rid
    $temp = Join-Path ([System.IO.Path]::GetTempPath()) "devcraft-install-$([Guid]::NewGuid())"
    $archive = Join-Path $temp "DevCraft-$rid.zip"
    $packageDir = Join-Path $temp "package"

    New-Item -ItemType Directory -Force -Path $temp, $packageDir | Out-Null
    $releaseTag = Get-ReleaseTag
    $assetUrl = Get-AssetUrl $rid $releaseTag

    Write-Host "Downloading DevCraft $releaseTag for $rid..."
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

function Main {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

    if ((Test-Path (Join-Path $scriptDir "devcraft.exe")) -and (Test-Path (Join-Path $scriptDir "profile"))) {
        Install-FromDirectory $scriptDir
    } else {
        Install-FromRelease
    }

    Add-ToPath
}

if (-not $env:DEVCRAFT_INSTALLER_TEST_MODE) {
    Main
}
