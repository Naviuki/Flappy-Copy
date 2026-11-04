# Builds the game for one platform and publishes it as a GitHub release asset.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/build_platform.ps1 -Platform windows
#
# Platforms: windows, linux, webgl, android (webgl and android need their Unity build support module).
# The archive is written to Builds\Release; add -Publish -Tag v1.0.1 -Name "Flappy Copy 1.0.1" to upload it.
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("windows", "linux", "webgl", "android")]
    [string]$Platform,
    [string]$UnityExe = "",
    [string]$ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [switch]$Publish,
    [string]$Tag = "",
    [string]$Name = "",
    [string]$Notes = "Release notes: see CHANGELOG.md",
    [int]$TimeoutMinutes = 90
)

$ErrorActionPreference = "Stop"

$map = @{
    windows = @{ Method = "BuildWindowsAndExit"; Title = "Windows x64"; Asset = "FlappyCopy-Windows.zip" }
    linux   = @{ Method = "BuildLinuxAndExit";   Title = "Linux x64";   Asset = "FlappyCopy-Linux.zip" }
    webgl   = @{ Method = "BuildWebGLAndExit";   Title = "WebGL";       Asset = "FlappyCopy-WebGL.zip" }
    android = @{ Method = "BuildAndroidAndExit"; Title = "Android";     Asset = "FlappyCopy-Android.apk" }
}
$target = $map[$Platform]

if ([string]::IsNullOrWhiteSpace($UnityExe)) {
    $UnityExe = $env:UNITY_PATH
}
if ([string]::IsNullOrWhiteSpace($UnityExe)) {
    $UnityExe = "D:\ProgramFiles\Unity\Editors\6000.5.1f1\Editor\Unity.exe"
}
if (-not (Test-Path $UnityExe)) {
    Write-Output "Unity not found: $UnityExe (pass -UnityExe <path to Unity.exe> or set UNITY_PATH)"
    exit 2
}

$logDir = Join-Path $env:TEMP "flappycopy_build"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$log = Join-Path $logDir ("build-" + $Platform + ".log")

Write-Output "Building $($target.Title) (log: $log)"
$process = Start-Process -FilePath $UnityExe -ArgumentList @(
    "-batchmode", "-nographics",
    "-projectPath", "`"$ProjectPath`"",
    "-executeMethod", "FlappyCopy.EditorTools.ReleaseBuilder.$($target.Method)",
    "-logFile", "`"$log`"",
    "-quit"
) -PassThru -WindowStyle Hidden

$closed = $process.WaitForExit($TimeoutMinutes * 60 * 1000)
if (-not $closed) {
    $process.Kill()
    Write-Output "Unity did not finish in $TimeoutMinutes minutes (log: $log)"
    exit 2
}

Select-String -Path $log -Pattern "\[FlappyCopy\] Release|Build succeeded|Build failed|error CS" |
    ForEach-Object { Write-Output $_.Line.Trim() }

$archive = Join-Path $ProjectPath ("Builds\Release\" + $target.Asset)
if (-not (Test-Path $archive)) {
    Write-Output "No archive was produced for $($target.Title) (log: $log)"
    exit 1
}

Write-Output "Archive: $archive ($((Get-Item $archive).Length) bytes)"
if ($env:GITHUB_OUTPUT) {
    "asset=$archive" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    "platform=$Platform" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
}

if ($Publish) {
    if ([string]::IsNullOrWhiteSpace($Tag)) {
        Write-Output "Pass -Tag <tag> to publish the archive as a release asset."
        exit 2
    }

    $gh = Get-Command gh -ErrorAction SilentlyContinue
    if (-not $gh) {
        Write-Output "GitHub CLI not found; install it to publish releases."
        exit 2
    }

    $exists = & $gh.Source release view $Tag --repo $env:GITHUB_REPOSITORY 2>$null
    if ($LASTEXITCODE -ne 0) {
        & $gh.Source release create $Tag $archive --repo $env:GITHUB_REPOSITORY --title $Name --notes $Notes
    }
    else {
        & $gh.Source release upload $Tag $archive --repo $env:GITHUB_REPOSITORY --clobber
    }

    exit $LASTEXITCODE
}

exit 0
