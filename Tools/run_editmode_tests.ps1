# Unity EditMode tests through the Unity Test Runner API.
# Starts Unity on the project (a normal editor window; the test run needs Unity running), waits for the run to
# write Temp/harness/editmode_results.json, reports the summary and closes the editor.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/run_editmode_tests.ps1 [-UnityExe <path>]
#
# If the editor session cannot start on this machine (licensing handshake), the script explains what to run
# instead: open the project, Window > General > Test Runner > EditMode > Run All.
param(
    [string]$UnityExe = "D:\ProgramFiles\Unity\Editors\6000.5.1f1\Editor\Unity.exe",
    [string]$ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [int]$RunTimeoutSeconds = 900
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path $UnityExe)) {
    Write-Output "Unity not found: $UnityExe (pass -UnityExe <path to Unity.exe>)"
    exit 2
}

$logDir = Join-Path $env:TEMP "flappycopy_tests"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$results = Join-Path $ProjectPath "Temp\harness\editmode_results"
foreach ($file in @("$results.json", "$results.log")) {
    if (Test-Path $file) { Remove-Item $file -Force }
}

$log = Join-Path $logDir "editmode.log"
if (Test-Path $log) { Remove-Item $log -Force }

Write-Output "Starting Unity (editor window; log: $log)"
Start-Process -FilePath $UnityExe -ArgumentList @("-projectPath", "`"$ProjectPath`"", "-logFile", "`"$log`"", "-executeMethod", "FlappyCopy.Tests.TestRunnerEntry.RunEditModeAndExit") | Out-Null

$deadline = (Get-Date).AddSeconds($RunTimeoutSeconds)
while ((Get-Date) -lt $deadline) {
    if (Test-Path "$results.json") {
        Start-Sleep -Seconds 2
        break
    }

    Start-Sleep -Seconds 2
}

if (-not (Test-Path "$results.json")) {
    Write-Output ""
    Write-Output "The EditMode run did not start on this machine."
    Write-Output "Unity was started in a normal window; if it is still open, run the tests from"
    Write-Output "Window > General > Test Runner > EditMode > Run All (see Tools/TESTS.md)."
    Get-Content $log -Tail 30 | Where-Object { $_ -match "FlappyCopy|error CS|Exception" }
    Get-Process | Where-Object { $_.ProcessName -eq "Unity" } | ForEach-Object { $_.CloseMainWindow() | Out-Null }
    exit 1
}

$summary = Get-Content "$results.json" -Raw
Write-Output $summary.Trim()
Get-Content "$results.log" -ErrorAction SilentlyContinue | Where-Object { $_ -like "FAIL*" } | ForEach-Object { Write-Output $_ }
Get-Process | Where-Object { $_.ProcessName -eq "Unity" } | ForEach-Object { $_.CloseMainWindow() | Out-Null }

if ($summary -match '"result":"passed"') {
    Write-Output "EditMode result: PASSED"
    exit 0
}

Write-Output "EditMode result: FAILED"
exit 1
