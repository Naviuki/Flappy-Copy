# Runs every EditMode [Test] of the project (same checks as the Unity Test Runner, invoked directly so it also
# works headless) and prints the summary. Results: Logs/harness/editmode_results.json (+ .log).
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/run_editmode_tests.ps1 [-UnityExe <path>]
#
# Exit code 0 = every test passed, 1 = at least one failure, 2 = Unity not found / did not finish.
param(
    [string]$UnityExe = "D:\ProgramFiles\Unity\Editors\6000.5.1f1\Editor\Unity.exe",
    [string]$ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [int]$TimeoutMinutes = 20
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path $UnityExe)) {
    Write-Output "Unity not found: $UnityExe (pass -UnityExe <path to Unity.exe>)"
    exit 2
}

$results = Join-Path $ProjectPath "Logs\harness\editmode_results"
foreach ($file in @("$results.json", "$results.log")) {
    if (Test-Path $file) { Remove-Item $file -Force }
}

$logDir = Join-Path $env:TEMP "flappycopy_tests"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$log = Join-Path $logDir ("editmode-" + (Get-Date).ToString("HHmmss") + ".log")

Write-Output "Running the EditMode tests (log: $log)"
$process = Start-Process -FilePath $UnityExe -ArgumentList @(
    "-batchmode", "-nographics",
    "-projectPath", "`"$ProjectPath`"",
    "-executeMethod", "FlappyCopy.Tests.EditModeTestEntry.RunAndExit",
    "-logFile", "`"$log`"",
    "-quit"
) -PassThru -WindowStyle Hidden

$closed = $process.WaitForExit($TimeoutMinutes * 60 * 1000)
if (-not $closed) {
    $process.Kill()
    Write-Output "Unity did not finish in $TimeoutMinutes minutes (log: $log)"
    exit 2
}

if (Test-Path "$results.json") {
    $summary = Get-Content "$results.json" -Raw
    Write-Output $summary.Trim()
    Get-Content "$results.log" -ErrorAction SilentlyContinue | Where-Object { $_ -like "FAIL*" } | ForEach-Object { Write-Output $_ }
    if ($summary -match '"result":"passed"') {
        Write-Output "EditMode result: PASSED"
        exit 0
    }

    Write-Output "EditMode result: FAILED (log: $log)"
    exit 1
}

Write-Output "No result file was written (log: $log). Last lines:"
Get-Content $log -Tail 30 -ErrorAction SilentlyContinue | Where-Object { $_ -match "FlappyCopy|error CS|Exception" }
exit 2
