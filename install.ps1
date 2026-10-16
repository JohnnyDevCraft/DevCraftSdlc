$ErrorActionPreference = "Stop"

$Repository = if ($env:DEVCRAFT_REPOSITORY) { $env:DEVCRAFT_REPOSITORY } else { "JohnnyDevCraft/DevCraftSdlc" }
$InstallDir = if ($env:DEVCRAFT_HOME) { $env:DEVCRAFT_HOME } else { Join-Path $HOME ".DevCraft" }

function Fail($Message) {
    throw "DevCraft install failed: $Message"
}

function Resolve-Rid($Readings) {
    foreach ($reading in $Readings) {
        $name = "$reading".Trim().ToUpperInvariant()

        if ($name -eq "AMD64" -or $name -eq "X64") {
            return "win-x64"
        }
    }

    return $null
}

function Get-Rid {
    # A 32-bit shell on 64-bit Windows reports the real processor in PROCESSOR_ARCHITEW6432.
    $readings = @($env:PROCESSOR_ARCHITEW6432, $env:PROCESSOR_ARCHITECTURE)

    try {
        $readings += [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture
    }
    catch {
        # Older .NET Framework builds do not have RuntimeInformation.
        $readings += "unavailable"
    }

    $rid = Resolve-Rid $readings

    if (-not $rid) {
        Fail "unsupported CPU architecture (PROCESSOR_ARCHITEW6432='$($readings[0])', PROCESSOR_ARCHITECTURE='$($readings[1])', OSArchitecture='$($readings[2])')"
    }

    return $rid
}

function Get-ProfileRelativePath($SourceRoot, $FullName) {
    return $FullName.Substring($SourceRoot.Length).TrimStart("\", "/")
}

function Copy-Profile($SourceProfile) {
    New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null

    if (Test-Path -LiteralPath $SourceProfile) {
        $sourceRoot = (Get-Item -LiteralPath $SourceProfile).FullName

        Get-ChildItem -LiteralPath $sourceRoot -Directory -Recurse | ForEach-Object {
            $relativePath = Get-ProfileRelativePath $sourceRoot $_.FullName
            New-Item -ItemType Directory -Force -Path (Join-Path $InstallDir $relativePath) | Out-Null
        }

        Get-ChildItem -LiteralPath $sourceRoot -File -Recurse | Where-Object { $_.Name -ne "soul.md" } | ForEach-Object {
            $relativePath = Get-ProfileRelativePath $sourceRoot $_.FullName
            $targetPath = Join-Path $InstallDir $relativePath
            # Seeded user data: copy only when missing so reinstalls never reset it.
            $isSeededUserData = @("configure.json", "features/projects.json") -contains $relativePath.Replace("\", "/")
            if (-not ($isSeededUserData -and (Test-Path -LiteralPath $targetPath))) {
                New-Item -ItemType Directory -Force -Path (Split-Path -Parent $targetPath) | Out-Null
                Copy-Item -LiteralPath $_.FullName -Destination $targetPath -Force
            }
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

function Remove-OldBinaries($TargetDirectory, $TargetName) {
    # A binary that was still running during an earlier install could not be deleted then.
    Get-ChildItem -LiteralPath $TargetDirectory -Filter ".$TargetName.*.old" -Force | ForEach-Object {
        Remove-OldBinary $_.FullName
    }
}

function Remove-OldBinary($Path) {
    try {
        Remove-Item -LiteralPath $Path -Force -ErrorAction Stop
    }
    catch {
        # Still running; the next install removes it.
    }
}

function Install-Binary($SourceBinary, $TargetBinary) {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $TargetBinary) | Out-Null
    # .NET file calls resolve relative paths against the process folder, not the PowerShell location.
    $targetDirectory = (Get-Item -LiteralPath (Split-Path -Parent $TargetBinary)).FullName
    $targetName = Split-Path -Leaf $TargetBinary
    $TargetBinary = Join-Path $targetDirectory $targetName
    $tempBinary = Join-Path $targetDirectory ".devcraft-install-$([Guid]::NewGuid()).tmp"
    $oldBinary = $null

    Remove-OldBinaries $targetDirectory $targetName

    try {
        Copy-Item -LiteralPath $SourceBinary -Destination $tempBinary -Force

        # Moving onto an existing devcraft.exe fails with "file already exists" when it cannot be
        # deleted, such as while it runs. Windows still allows renaming it, so move it aside first.
        if (Test-Path -LiteralPath $TargetBinary) {
            $oldBinary = Join-Path $targetDirectory ".$targetName.$([Guid]::NewGuid()).old"
            [System.IO.File]::Move($TargetBinary, $oldBinary)
        }

        try {
            [System.IO.File]::Move($tempBinary, $TargetBinary)
        }
        catch {
            if ($oldBinary) {
                [System.IO.File]::Move($oldBinary, $TargetBinary)
                $oldBinary = $null
            }

            throw
        }
    }
    finally {
        if (Test-Path -LiteralPath $tempBinary) {
            Remove-Item -LiteralPath $tempBinary -Force
        }
    }

    if ($oldBinary) {
        Remove-OldBinary $oldBinary
    }
}

function Install-FromDirectory($PackageDir) {
    $binary = Join-Path $PackageDir "devcraft.exe"

    if (-not (Test-Path $binary)) {
        Fail "package binary not found"
    }

    Copy-Profile (Join-Path $PackageDir "profile")
    Install-Binary $binary (Join-Path $InstallDir "devcraft.exe")
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
    Invoke-WebRequest $assetUrl -OutFile $archive -UseBasicParsing
    Expand-Archive $archive -DestinationPath $packageDir -Force
    Install-FromDirectory $packageDir
}

function Add-ToPath {
    Write-Host "DevCraft installed in $InstallDir"

    if ($env:DEVCRAFT_NO_MODIFY_PATH) {
        Write-Host "PATH was not changed. Add $InstallDir to PATH, then run: devcraft"
        return
    }

    $currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
    $paths = $currentPath -split ";"

    if ($paths -notcontains $InstallDir) {
        $newPath = if ([string]::IsNullOrWhiteSpace($currentPath)) { $InstallDir } else { "$currentPath;$InstallDir" }
        [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
    }

    Write-Host "Open a new PowerShell window, then run: devcraft"
}

function Test-LocalPackage($Directory) {
    # Piping the script to Invoke-Expression leaves no script folder to inspect.
    if ([string]::IsNullOrWhiteSpace($Directory)) {
        return $false
    }

    return (Test-Path (Join-Path $Directory "devcraft.exe")) -and (Test-Path (Join-Path $Directory "profile"))
}

function Main {
    if (Test-LocalPackage $PSScriptRoot) {
        Install-FromDirectory $PSScriptRoot
    } else {
        Install-FromRelease
    }

    Add-ToPath
}

if (-not $env:DEVCRAFT_INSTALLER_TEST_MODE) {
    Main
}
