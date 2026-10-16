$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$installer = Join-Path $repoRoot "install.ps1"
$shell = (Get-Process -Id $PID).Path

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

function New-TestRoot {
    $root = Join-Path ([System.IO.Path]::GetTempPath()) "devcraft-installer-e2e-$([Guid]::NewGuid())"
    New-Item -ItemType Directory -Force -Path $root | Out-Null
    return $root
}

function New-FakePackage($Root, $Version = "") {
    $package = Join-Path $Root "package$Version"
    $sourceProfile = Join-Path $package "profile"
    New-Item -ItemType Directory -Force -Path (Join-Path $sourceProfile "templates") | Out-Null

    Set-Content (Join-Path $package "devcraft.exe") "fake binary$Version"
    Set-Content (Join-Path $sourceProfile "DevCraft.md") "rules$Version"
    Set-Content (Join-Path (Join-Path $sourceProfile "templates") "spec.template.md") "template$Version"
    Set-Content (Join-Path $sourceProfile "soul.md") "package soul"
    Set-Content (Join-Path $sourceProfile "configure.json") "package configuration"
    New-Item -ItemType Directory -Force -Path (Join-Path $sourceProfile "features") | Out-Null
    Set-Content (Join-Path (Join-Path $sourceProfile "features") "projects.json") "package projects"
    Copy-Item $installer (Join-Path $package "install.ps1")

    return $package
}

function ConvertTo-SingleQuoted($Value) {
    return "'" + $Value.Replace("'", "''") + "'"
}

# Runs the installer in a child shell so the real entry point executes the way a user runs it.
function Invoke-InstallerShell($InstallDir, $ShellArguments) {
    $savedHome = $env:DEVCRAFT_HOME
    $savedNoModifyPath = $env:DEVCRAFT_NO_MODIFY_PATH
    $savedTestMode = $env:DEVCRAFT_INSTALLER_TEST_MODE
    $savedPreference = $ErrorActionPreference

    $env:DEVCRAFT_HOME = $InstallDir
    $env:DEVCRAFT_NO_MODIFY_PATH = "1"
    $env:DEVCRAFT_INSTALLER_TEST_MODE = $null

    try {
        # Windows PowerShell turns redirected native stderr into terminating errors under "Stop".
        $ErrorActionPreference = "Continue"
        $output = & $shell -NoProfile -NonInteractive @ShellArguments 2>&1 | Out-String

        return [PSCustomObject]@{ ExitCode = $LASTEXITCODE; Output = $output }
    }
    finally {
        $ErrorActionPreference = $savedPreference
        $env:DEVCRAFT_HOME = $savedHome
        $env:DEVCRAFT_NO_MODIFY_PATH = $savedNoModifyPath
        $env:DEVCRAFT_INSTALLER_TEST_MODE = $savedTestMode
    }
}

function Test-PipedScriptSelectsReleaseInstall {
    $root = New-TestRoot
    $installDir = Join-Path $root "install-root"
    $marker = Join-Path $root "release-route.txt"

    # The README command pipes the script text to Invoke-Expression, so no script file exists.
    $command = @(
        "`$env:DEVCRAFT_INSTALLER_TEST_MODE = '1'"
        "Get-Content -Raw -LiteralPath $(ConvertTo-SingleQuoted $installer) | Invoke-Expression"
        "function Install-FromRelease { Set-Content -LiteralPath $(ConvertTo-SingleQuoted $marker) -Value 'release' }"
        'Main'
    ) -join "; "

    $result = Invoke-InstallerShell $installDir @("-Command", $command)

    Assert-Equal 0 $result.ExitCode "piped installer exits cleanly: $($result.Output)"
    Assert-True (Test-Path $marker) "piped installer installs from the release when no script file exists"
}

function Test-ScriptFileInstallsLocalPackage {
    $root = New-TestRoot
    $installDir = Join-Path $root "install-root"
    $package = New-FakePackage $root
    New-Item -ItemType Directory -Force -Path $installDir | Out-Null
    Set-Content (Join-Path $installDir "configure.json") "existing configuration"
    New-Item -ItemType Directory -Force -Path (Join-Path $installDir "features") | Out-Null
    Set-Content (Join-Path (Join-Path $installDir "features") "projects.json") "existing projects"

    $result = Invoke-InstallerShell $installDir @("-ExecutionPolicy", "Bypass", "-File", (Join-Path $package "install.ps1"))

    Assert-Equal 0 $result.ExitCode "package installer exits cleanly: $($result.Output)"
    Assert-Equal "fake binary" (Get-Content (Join-Path $installDir "devcraft.exe") -Raw).Trim() "package installer installs the package binary"
    Assert-True (Test-Path (Join-Path $installDir "DevCraft.md")) "package installer copies root profile files"
    Assert-True (Test-Path (Join-Path (Join-Path $installDir "templates") "spec.template.md")) "package installer keeps nested profile paths"
    Assert-True (-not (Test-Path (Join-Path $installDir "soul.md"))) "package installer does not copy soul.md"
    Assert-Equal "existing configuration" (Get-Content (Join-Path $installDir "configure.json") -Raw).Trim() "package installer preserves an existing configure.json"
    Assert-Equal "existing projects" (Get-Content (Join-Path (Join-Path $installDir "features") "projects.json") -Raw).Trim() "package installer preserves an existing features/projects.json"

    if ($env:OS -eq "Windows_NT") {
        $userPath = [Environment]::GetEnvironmentVariable("Path", "User")
        Assert-True (-not ("$userPath" -split ";" -contains $installDir)) "DEVCRAFT_NO_MODIFY_PATH leaves the user Path unchanged"
    }
}

function Get-InstallLeftovers($InstallDir) {
    return @(Get-ChildItem -LiteralPath $InstallDir -Force | Where-Object { $_.Name -like ".devcraft-install-*" -or $_.Name -like "*.old" })
}

function Test-ReinstallUpdatesBinaryAndProfile {
    $root = New-TestRoot
    $installDir = Join-Path $root "install-root"
    $first = New-FakePackage $root " v1"
    $second = New-FakePackage $root " v2"

    $result = Invoke-InstallerShell $installDir @("-ExecutionPolicy", "Bypass", "-File", (Join-Path $first "install.ps1"))
    Assert-Equal 0 $result.ExitCode "first install exits cleanly: $($result.Output)"

    Set-Content (Join-Path $installDir "soul.md") "my soul"
    Set-Content (Join-Path $installDir "configure.json") "my configuration"
    Set-Content (Join-Path (Join-Path $installDir "features") "projects.json") "my projects"

    $result = Invoke-InstallerShell $installDir @("-ExecutionPolicy", "Bypass", "-File", (Join-Path $second "install.ps1"))

    Assert-Equal 0 $result.ExitCode "reinstall exits cleanly: $($result.Output)"
    Assert-Equal "fake binary v2" (Get-Content (Join-Path $installDir "devcraft.exe") -Raw).Trim() "reinstall replaces devcraft.exe"
    Assert-Equal "rules v2" (Get-Content (Join-Path $installDir "DevCraft.md") -Raw).Trim() "reinstall refreshes DevCraft.md"
    Assert-Equal "template v2" (Get-Content (Join-Path (Join-Path $installDir "templates") "spec.template.md") -Raw).Trim() "reinstall refreshes templates"
    Assert-Equal "my soul" (Get-Content (Join-Path $installDir "soul.md") -Raw).Trim() "reinstall preserves soul.md"
    Assert-Equal "my configuration" (Get-Content (Join-Path $installDir "configure.json") -Raw).Trim() "reinstall preserves configure.json"
    Assert-Equal "my projects" (Get-Content (Join-Path (Join-Path $installDir "features") "projects.json") -Raw).Trim() "reinstall preserves features/projects.json"
    Assert-Equal 0 (Get-InstallLeftovers $installDir).Count "reinstall leaves no temporary or old binaries"
}

function Test-ReinstallReplacesRunningBinary {
    if ($env:OS -ne "Windows_NT") {
        return
    }

    $root = New-TestRoot
    $installDir = Join-Path $root "install-root"
    $package = New-FakePackage $root
    $runningBinary = Join-Path $installDir "devcraft.exe"
    New-Item -ItemType Directory -Force -Path $installDir | Out-Null

    # A running executable can be renamed but not deleted, the same as an open DevCraft session.
    Copy-Item -LiteralPath (Join-Path $env:SystemRoot "System32\PING.EXE") -Destination $runningBinary
    $process = Start-Process -FilePath $runningBinary -ArgumentList "-n", "120", "127.0.0.1" -WindowStyle Hidden -PassThru

    try {
        $result = Invoke-InstallerShell $installDir @("-ExecutionPolicy", "Bypass", "-File", (Join-Path $package "install.ps1"))

        Assert-Equal 0 $result.ExitCode "install over a running devcraft.exe exits cleanly: $($result.Output)"
        Assert-Equal "fake binary" (Get-Content $runningBinary -Raw).Trim() "install over a running devcraft.exe replaces it"
    }
    finally {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        $process.WaitForExit()
    }

    $result = Invoke-InstallerShell $installDir @("-ExecutionPolicy", "Bypass", "-File", (Join-Path $package "install.ps1"))

    Assert-Equal 0 $result.ExitCode "install after the old binary exits cleanly: $($result.Output)"
    Assert-Equal 0 (Get-InstallLeftovers $installDir).Count "the next install removes the old binary"
}

function Test-InstallerAvoidsWindowsPowerShellIncompatibleCalls {
    $lines = Get-Content $installer

    # Windows PowerShell 5.1 runs on .NET Framework, which has no Path.GetRelativePath.
    Assert-True (-not ($lines -match "GetRelativePath")) "installer does not call Path.GetRelativePath"

    # Windows PowerShell 5.1 prompts for confirmation unless Invoke-WebRequest uses basic parsing.
    $webRequests = @($lines -match "Invoke-WebRequest")
    Assert-True ($webRequests.Count -gt 0) "installer downloads the release with Invoke-WebRequest"

    foreach ($line in $webRequests) {
        Assert-True ($line -match "-UseBasicParsing") "Invoke-WebRequest uses -UseBasicParsing: $($line.Trim())"
    }
}

$failures = @()
$tests = @(
    "Test-PipedScriptSelectsReleaseInstall",
    "Test-ScriptFileInstallsLocalPackage",
    "Test-ReinstallUpdatesBinaryAndProfile",
    "Test-ReinstallReplacesRunningBinary",
    "Test-InstallerAvoidsWindowsPowerShellIncompatibleCalls"
)

foreach ($test in $tests) {
    try {
        & $test
        Write-Host "passed: $test"
    }
    catch {
        $failures += "$test - $($_.Exception.Message)"
        Write-Host "FAILED: $test - $($_.Exception.Message)"
    }
}

if ($failures.Count -gt 0) {
    throw "InstallerEndToEnd.Tests.ps1 failed: $($failures.Count) of $($tests.Count)"
}

Write-Host "InstallerEndToEnd.Tests.ps1 passed"
